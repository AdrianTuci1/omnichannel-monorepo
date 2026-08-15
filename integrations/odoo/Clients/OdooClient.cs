using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OdooBridge.Configuration;
using OdooBridge.Models;

namespace OdooBridge.Clients;

/// <summary>
/// Client HTTP pentru API-ul extern Odoo (JSON-RPC 2.0).
/// Endpoint-ul este configurabil prin <see cref="OdooOptions.BaseUrl"/>.
/// </summary>
public sealed class OdooClient
{
    private readonly HttpClient _http;
    private readonly OdooOptions _options;
    private readonly JsonSerializerOptions _json;
    private int _requestId;
    private int? _uid;

    public OdooClient(HttpClient http, IOptions<OdooOptions> options)
    {
        _http = http;
        _options = options.Value;
        _json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    /// <summary>Autentificare folosind perechea utilizator/API key și reține uid-ul de sesiune.</summary>
    public async Task<int> AuthenticateAsync(CancellationToken ct = default)
    {
        var request = BuildRequest("common", "authenticate",
            JsonSerializer.SerializeToElement(_options.Database, _json),
            JsonSerializer.SerializeToElement(_options.Username, _json),
            JsonSerializer.SerializeToElement(_options.ApiKey, _json),
            JsonSerializer.SerializeToElement(new { }, _json));

        var response = await CallAsync(request, ct);
        _uid = response.Result!.Value.GetInt32();
        return _uid.Value;
    }

    /// <summary>Returnează produsele active din Odoo prin search_read pe modelul configurat.</summary>
    public async Task<IReadOnlyList<OdooProduct>> SearchReadProductsAsync(CancellationToken ct = default)
    {
        var uid = await EnsureUidAsync(ct);

        var domain = JsonSerializer.SerializeToElement(
            new object[] { new object[] { "active", "=", true } }, _json);
        var fields = JsonSerializer.SerializeToElement(new[]
        {
            "id", "default_code", "name", "list_price", "currency_id",
            "description_sale", "active", "categ_id", "write_date"
        }, _json);
        var kwargs = JsonSerializer.SerializeToElement(new { limit = _options.PageSize }, _json);

        var request = BuildRequest("object", "execute_kw",
            JsonSerializer.SerializeToElement(_options.Database, _json),
            JsonSerializer.SerializeToElement(uid, _json),
            JsonSerializer.SerializeToElement(_options.ApiKey, _json),
            JsonSerializer.SerializeToElement(_options.ProductModel, _json),
            JsonSerializer.SerializeToElement("search_read", _json),
            domain, fields, kwargs);

        var response = await CallAsync(request, ct);
        return response.Result!.Value.Deserialize<IReadOnlyList<OdooProduct>>(_json)
            ?? Array.Empty<OdooProduct>();
    }

    /// <summary>Returnează SKU-urile variantelor de produs (product.product) pentru un set de ID-uri.</summary>
    public async Task<IReadOnlyList<OdooProductVariant>> SearchReadProductVariantsAsync(
        IReadOnlyCollection<int> productIds, CancellationToken ct = default)
    {
        if (productIds.Count == 0)
            return Array.Empty<OdooProductVariant>();

        var uid = await EnsureUidAsync(ct);

        var domain = JsonSerializer.SerializeToElement(
            new object[] { new object[] { "id", "in", productIds.ToArray() } }, _json);
        var fields = JsonSerializer.SerializeToElement(new[] { "id", "default_code" }, _json);
        var kwargs = JsonSerializer.SerializeToElement(new { }, _json);

        var request = BuildRequest("object", "execute_kw",
            JsonSerializer.SerializeToElement(_options.Database, _json),
            JsonSerializer.SerializeToElement(uid, _json),
            JsonSerializer.SerializeToElement(_options.ApiKey, _json),
            JsonSerializer.SerializeToElement("product.product", _json),
            JsonSerializer.SerializeToElement("search_read", _json),
            domain, fields, kwargs);

        var response = await CallAsync(request, ct);
        return response.Result!.Value.Deserialize<IReadOnlyList<OdooProductVariant>>(_json)
            ?? Array.Empty<OdooProductVariant>();
    }

    /// <summary>Returnează comenzile recente din Odoo prin search_read pe modelul configurat.</summary>
    public async Task<IReadOnlyList<OdooOrder>> SearchReadOrdersAsync(CancellationToken ct = default)
    {
        var uid = await EnsureUidAsync(ct);

        var domain = JsonSerializer.SerializeToElement(
            new object[] { new object[] { "state", "not in", new[] { "draft", "cancel" } } }, _json);
        var fields = JsonSerializer.SerializeToElement(new[]
        {
            "id", "name", "partner_id", "state", "currency_id",
            "order_line", "date_order", "amount_total"
        }, _json);
        var kwargs = JsonSerializer.SerializeToElement(new { limit = _options.PageSize }, _json);

        var request = BuildRequest("object", "execute_kw",
            JsonSerializer.SerializeToElement(_options.Database, _json),
            JsonSerializer.SerializeToElement(uid, _json),
            JsonSerializer.SerializeToElement(_options.ApiKey, _json),
            JsonSerializer.SerializeToElement(_options.OrderModel, _json),
            JsonSerializer.SerializeToElement("search_read", _json),
            domain, fields, kwargs);

        var response = await CallAsync(request, ct);
        return response.Result!.Value.Deserialize<IReadOnlyList<OdooOrder>>(_json)
            ?? Array.Empty<OdooOrder>();
    }

    /// <summary>Returnează comanda Odoo (sale.order) cu numele dat, sau null dacă nu există.</summary>
    public async Task<OdooOrder?> SearchReadOrderByNameAsync(string name, CancellationToken ct = default)
    {
        var uid = await EnsureUidAsync(ct);

        var domain = JsonSerializer.SerializeToElement(
            new object[] { new object[] { "name", "=", name } }, _json);
        var fields = JsonSerializer.SerializeToElement(new[] { "id", "name", "state" }, _json);
        var kwargs = JsonSerializer.SerializeToElement(new { limit = 1 }, _json);

        var request = BuildRequest("object", "execute_kw",
            JsonSerializer.SerializeToElement(_options.Database, _json),
            JsonSerializer.SerializeToElement(uid, _json),
            JsonSerializer.SerializeToElement(_options.ApiKey, _json),
            JsonSerializer.SerializeToElement(_options.OrderModel, _json),
            JsonSerializer.SerializeToElement("search_read", _json),
            domain, fields, kwargs);

        var response = await CallAsync(request, ct);
        var orders = response.Result!.Value.Deserialize<IReadOnlyList<OdooOrder>>(_json)
            ?? Array.Empty<OdooOrder>();
        return orders.FirstOrDefault();
    }

    /// <summary>Actualizează starea unei comenzi Odoo (sale.order) prin metoda write.</summary>
    public async Task WriteOrderStateAsync(int orderId, string state, CancellationToken ct = default)
    {
        var uid = await EnsureUidAsync(ct);

        var ids = JsonSerializer.SerializeToElement(new[] { orderId }, _json);
        var values = JsonSerializer.SerializeToElement(new { state }, _json);

        var request = BuildRequest("object", "execute_kw",
            JsonSerializer.SerializeToElement(_options.Database, _json),
            JsonSerializer.SerializeToElement(uid, _json),
            JsonSerializer.SerializeToElement(_options.ApiKey, _json),
            JsonSerializer.SerializeToElement(_options.OrderModel, _json),
            JsonSerializer.SerializeToElement("write", _json),
            ids, values);

        await CallAsync(request, ct);
    }

    /// <summary>Returnează liniile de comandă pentru un set de ID-uri.</summary>
    public async Task<IReadOnlyList<OdooOrderLine>> SearchReadOrderLinesAsync(
        IReadOnlyCollection<int> orderIds, CancellationToken ct = default)
    {
        if (orderIds.Count == 0)
            return Array.Empty<OdooOrderLine>();

        var uid = await EnsureUidAsync(ct);

        var domain = JsonSerializer.SerializeToElement(
            new object[] { new object[] { "order_id", "in", orderIds.ToArray() } }, _json);
        var fields = JsonSerializer.SerializeToElement(new[]
        {
            "id", "order_id", "product_id", "name", "product_uom_qty", "price_unit"
        }, _json);
        var kwargs = JsonSerializer.SerializeToElement(new { }, _json);

        var request = BuildRequest("object", "execute_kw",
            JsonSerializer.SerializeToElement(_options.Database, _json),
            JsonSerializer.SerializeToElement(uid, _json),
            JsonSerializer.SerializeToElement(_options.ApiKey, _json),
            JsonSerializer.SerializeToElement("sale.order.line", _json),
            JsonSerializer.SerializeToElement("search_read", _json),
            domain, fields, kwargs);

        var response = await CallAsync(request, ct);
        return response.Result!.Value.Deserialize<IReadOnlyList<OdooOrderLine>>(_json)
            ?? Array.Empty<OdooOrderLine>();
    }

    /// <summary>Returnează partenerii (clienții) pentru un set de ID-uri.</summary>
    public async Task<IReadOnlyList<OdooPartner>> SearchReadPartnersAsync(
        IReadOnlyCollection<int> partnerIds, CancellationToken ct = default)
    {
        if (partnerIds.Count == 0)
            return Array.Empty<OdooPartner>();

        var uid = await EnsureUidAsync(ct);

        var domain = JsonSerializer.SerializeToElement(
            new object[] { new object[] { "id", "in", partnerIds.ToArray() } }, _json);
        var fields = JsonSerializer.SerializeToElement(new[] { "id", "name", "email", "phone" }, _json);
        var kwargs = JsonSerializer.SerializeToElement(new { }, _json);

        var request = BuildRequest("object", "execute_kw",
            JsonSerializer.SerializeToElement(_options.Database, _json),
            JsonSerializer.SerializeToElement(uid, _json),
            JsonSerializer.SerializeToElement(_options.ApiKey, _json),
            JsonSerializer.SerializeToElement(_options.PartnerModel, _json),
            JsonSerializer.SerializeToElement("search_read", _json),
            domain, fields, kwargs);

        var response = await CallAsync(request, ct);
        return response.Result!.Value.Deserialize<IReadOnlyList<OdooPartner>>(_json)
            ?? Array.Empty<OdooPartner>();
    }

    private async Task<int> EnsureUidAsync(CancellationToken ct)
        => _uid ?? await AuthenticateAsync(ct);

    private JsonRpcRequest BuildRequest(string service, string method, params JsonElement[] args)
    {
        var id = Interlocked.Increment(ref _requestId);
        return new JsonRpcRequest
        {
            Id = id,
            Params = new JsonRpcParams
            {
                Service = service,
                Method = method,
                Args = args
            }
        };
    }

    private async Task<JsonRpcResponse> CallAsync(JsonRpcRequest request, CancellationToken ct)
    {
        using var httpResponse = await _http.PostAsJsonAsync(_options.JsonRpcEndpoint, request, _json, ct);
        httpResponse.EnsureSuccessStatusCode();

        var response = await httpResponse.Content.ReadFromJsonAsync<JsonRpcResponse>(_json, ct)
            ?? throw new InvalidOperationException("Răspuns JSON-RPC gol de la Odoo.");

        if (!response.IsSuccess)
            throw new InvalidOperationException(
                $"Odoo a returnat eroare JSON-RPC {response.Error!.Code}: {response.Error.Message}");

        return response;
    }
}
