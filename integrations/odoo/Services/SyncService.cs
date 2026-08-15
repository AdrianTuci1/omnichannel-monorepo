using System.Text.Json;
using Microsoft.Extensions.Logging;
using OdooBridge.Clients;
using OdooBridge.Models;

namespace OdooBridge.Services;

/// <summary>
/// Logica de sincronizare de bază între Odoo și store-api:
/// produse potrivite după SKU și comenzi create idempotent (marcator în câmpul notes).
/// </summary>
public sealed class SyncService
{
    private readonly OdooClient _odoo;
    private readonly StoreApiClient _store;
    private readonly ILogger<SyncService> _logger;

    public SyncService(OdooClient odoo, StoreApiClient store, ILogger<SyncService> logger)
    {
        _odoo = odoo;
        _store = store;
        _logger = logger;
    }

    public async Task<SyncReport> SyncProductsAsync(CancellationToken ct)
    {
        var odooProducts = await _odoo.SearchReadProductsAsync(ct);
        var storeProducts = await _store.GetProductsAsync(ct);

        var bySku = BuildSkuIndex(storeProducts);
        var report = new SyncReport();

        foreach (var product in odooProducts)
        {
            if (string.IsNullOrWhiteSpace(product.Sku))
            {
                _logger.LogWarning("Produs Odoo {OdooId} nu are default_code (SKU) și a fost omis.", product.Id);
                report.Skipped++;
                continue;
            }

            var currency = ResolveCurrency(product.CurrencyRef);

            if (bySku.TryGetValue(product.Sku, out var existing))
            {
                await _store.UpdateProductAsync(existing.Id, new UpdateProductRequest
                {
                    Name = product.Name,
                    PriceAmount = product.PriceAmount,
                    PriceCurrency = currency,
                    CategoryId = existing.CategoryId,
                    Description = product.Description
                }, ct);
                report.Updated++;
            }
            else
            {
                await _store.CreateProductAsync(new CreateProductRequest
                {
                    Sku = product.Sku,
                    Name = product.Name,
                    PriceAmount = product.PriceAmount,
                    PriceCurrency = currency,
                    Description = product.Description
                }, ct);
                report.Created++;
            }
        }

        return report;
    }

    public async Task<SyncReport> SyncOrdersAsync(CancellationToken ct)
    {
        var odooOrders = await _odoo.SearchReadOrdersAsync(ct);
        if (odooOrders.Count == 0)
            return new SyncReport();

        var orderLineIds = odooOrders.SelectMany(o => o.OrderLineIds).Distinct().ToArray();
        var orderLines = await _odoo.SearchReadOrderLinesAsync(orderLineIds, ct);

        var partnerIds = odooOrders.Select(o => OdooRefs.GetId(o.PartnerRef)).Where(id => id > 0).Distinct().ToArray();
        var partners = await _odoo.SearchReadPartnersAsync(partnerIds, ct);
        var partnerById = partners.ToDictionary(p => p.Id);

        var variantIds = orderLines.Select(l => OdooRefs.GetId(l.ProductRef)).Where(id => id > 0).Distinct().ToArray();
        var variants = await _odoo.SearchReadProductVariantsAsync(variantIds, ct);
        var skuByVariantId = variants
            .Where(v => !string.IsNullOrWhiteSpace(v.Sku))
            .ToDictionary(v => v.Id, v => v.Sku);

        var storeProducts = await _store.GetProductsAsync(ct);
        var productBySku = BuildSkuIndex(storeProducts);

        var storeCustomers = await _store.GetCustomersAsync(ct);
        var customerByEmail = storeCustomers
            .Where(c => !string.IsNullOrWhiteSpace(c.Email))
            .GroupBy(c => c.Email, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var storeOrders = await _store.GetOrdersAsync(ct);
        var existingMarkers = storeOrders
            .Where(o => !string.IsNullOrWhiteSpace(o.Notes))
            .Select(o => o.Notes!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var report = new SyncReport();

        foreach (var order in odooOrders)
        {
            var marker = BuildMarker(order.Name);
            if (existingMarkers.Contains(marker))
            {
                report.Skipped++;
                continue;
            }

            var partnerId = OdooRefs.GetId(order.PartnerRef);
            if (!partnerById.TryGetValue(partnerId, out var partner) || string.IsNullOrWhiteSpace(partner.Email))
            {
                _logger.LogWarning("Comanda Odoo {Order} nu are partener cu email valid și a fost omisă.", order.Name);
                report.Skipped++;
                continue;
            }

            var customer = await ResolveCustomerAsync(partner, customerByEmail, ct);

            var lines = new List<CreateOrderLineRequest>();
            foreach (var line in orderLines.Where(l => OdooRefs.GetId(l.OrderRef) == order.Id))
            {
                var variantId = OdooRefs.GetId(line.ProductRef);
                if (variantId <= 0 || !skuByVariantId.TryGetValue(variantId, out var sku))
                    continue;

                if (!productBySku.TryGetValue(sku, out var product))
                {
                    _logger.LogWarning(
                        "Linia {LineId} din comanda {Order} referă SKU {Sku} inexistent în store și a fost omisă.",
                        line.Id, order.Name, sku);
                    continue;
                }

                var quantity = (int)Math.Round(line.Quantity, MidpointRounding.AwayFromZero);
                if (quantity < 1)
                    continue;

                lines.Add(new CreateOrderLineRequest { ProductId = product.Id, Quantity = quantity });
            }

            if (lines.Count == 0)
            {
                _logger.LogWarning("Comanda Odoo {Order} nu are linii mapabile în store și a fost omisă.", order.Name);
                report.Skipped++;
                continue;
            }

            await _store.CreateOrderAsync(new CreateOrderRequest
            {
                CustomerId = customer.Id,
                Currency = ResolveCurrency(order.CurrencyRef),
                Notes = marker,
                Lines = lines
            }, ct);

            existingMarkers.Add(marker);
            report.Created++;
        }

        return report;
    }

    public async Task<SyncReport> SyncOrdersBackAsync(CancellationToken ct)
    {
        var storeOrders = await _store.GetOrdersAsync(ct);
        var report = new SyncReport();

        foreach (var order in storeOrders)
        {
            var odooOrderName = ParseOdooMarker(order.Notes);
            if (odooOrderName is null)
            {
                // Comanda nu provine din Odoo (fără marcator) — nu se propagă înapoi.
                report.Skipped++;
                continue;
            }

            var odooState = MapStoreStatusToOdooState(order.Status);
            if (odooState is null)
            {
                // Statusul store nu are corespondent de propagat (ex. Draft — starea inițială).
                report.Skipped++;
                continue;
            }

            var odooOrder = await _odoo.SearchReadOrderByNameAsync(odooOrderName, ct);
            if (odooOrder is null)
            {
                _logger.LogWarning(
                    "Comanda store {OrderNumber} referă Odoo {OdooName} inexistent și a fost omisă.",
                    order.OrderNumber, odooOrderName);
                report.Skipped++;
                continue;
            }

            if (string.Equals(odooOrder.State, odooState, StringComparison.OrdinalIgnoreCase))
            {
                report.Skipped++;
                continue;
            }

            await _odoo.WriteOrderStateAsync(odooOrder.Id, odooState, ct);
            report.Updated++;
        }

        return report;
    }

    private async Task<StoreCustomer> ResolveCustomerAsync(
        OdooPartner partner,
        Dictionary<string, StoreCustomer> customerByEmail,
        CancellationToken ct)
    {
        if (customerByEmail.TryGetValue(partner.Email!, out var existing))
            return existing;

        var (firstName, lastName) = SplitName(partner.Name);
        var created = await _store.CreateCustomerAsync(new CreateCustomerRequest
        {
            Email = partner.Email!,
            FirstName = firstName,
            LastName = lastName,
            Phone = partner.Phone
        }, ct);

        customerByEmail[created.Email] = created;
        return created;
    }

    private static Dictionary<string, StoreProduct> BuildSkuIndex(IReadOnlyList<StoreProduct> products)
        => products
            .Where(p => !string.IsNullOrWhiteSpace(p.Sku))
            .GroupBy(p => p.Sku, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

    private static string BuildMarker(string odooOrderName) => $"Odoo:{odooOrderName}";

    private static string? ParseOdooMarker(string? notes)
    {
        const string prefix = "Odoo:";
        if (string.IsNullOrWhiteSpace(notes) || !notes.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var name = notes[prefix.Length..].Trim();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    /// <summary>
    /// Mapează statusul store-api (numele enum-ului OrderStatus) pe starea Odoo (sale.order.state).
    /// Returnează null pentru stările care nu trebuie propagate (Draft — starea inițială a comenzilor
    /// create în store în urma sincronizării forward, pentru a nu regresa starea din Odoo).
    /// </summary>
    private static string? MapStoreStatusToOdooState(string storeStatus) => storeStatus switch
    {
        "Pending" => "sent",
        "Paid" => "sale",
        "Shipped" => "done",
        "Delivered" => "done",
        "Cancelled" => "cancel",
        _ => null,
    };

    private static string ResolveCurrency(JsonElement currencyRef)
    {
        var code = OdooRefs.GetName(currencyRef);
        return string.IsNullOrWhiteSpace(code) ? "USD" : code.Trim().ToUpperInvariant();
    }

    private static (string FirstName, string LastName) SplitName(string fullName)
    {
        var parts = (fullName ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => ("Client", "Odoo"),
            1 => (parts[0], parts[0]),
            _ => (parts[0], string.Join(' ', parts.Skip(1)))
        };
    }
}
