using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pgvector;
using Recommender.Api.Clients;
using Recommender.Api.Domain;
using Recommender.Api.Embeddings;

namespace Recommender.Api.Persistence;

/// <summary>
/// Încarcă catalogul și comenzile reale din Store API în <see cref="RecommenderDbContext"/>.
/// </summary>
public interface IStoreDataSynchronizer
{
    /// <summary>
    /// Asigură că datele sunt încărcate. Idempotent; reîncearcă la cererile ulterioare
    /// dacă Store API-ul nu era disponibil la prima încercare.
    /// </summary>
    Task EnsureLoadedAsync(CancellationToken ct = default);
}

public sealed class StoreDataSynchronizer : IStoreDataSynchronizer, IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IStoreApiClient _storeApi;
    private readonly IEmbeddingService _embedding;
    private readonly IEmbeddingStore _embeddings;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _loaded;

    public StoreDataSynchronizer(
        IServiceScopeFactory scopeFactory,
        IStoreApiClient storeApi,
        IEmbeddingService embedding,
        IEmbeddingStore embeddings)
    {
        _scopeFactory = scopeFactory;
        _storeApi = storeApi;
        _embedding = embedding;
        _embeddings = embeddings;
    }

    public Task StartAsync(CancellationToken cancellationToken)
        => EnsureLoadedAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task EnsureLoadedAsync(CancellationToken ct = default)
    {
        if (_loaded)
            return;

        await _gate.WaitAsync(ct);
        try
        {
            if (_loaded)
                return;

            await LoadAsync(ct);
            _loaded = true;
        }
        catch (Exception)
        {
            // Store API indisponibil la pornire; datele rămân neîncărcate și se reîncearcă
            // leneș la următoarea cerere de recomandare.
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var products = await _storeApi.GetProductsAsync(ct);
        var orders = await _storeApi.GetOrdersAsync(ct);

        var embeddings = new Dictionary<Guid, Vector>();
        foreach (var product in products)
            embeddings[product.Id] = _embedding.Embed(BuildEmbeddingText(product));

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RecommenderDbContext>();

        db.OrderLines.RemoveRange(await db.OrderLines.ToListAsync(ct));
        db.Orders.RemoveRange(await db.Orders.ToListAsync(ct));
        db.Products.RemoveRange(await db.Products.ToListAsync(ct));

        foreach (var product in products)
            db.Products.Add(MapProduct(product));

        foreach (var order in orders)
        {
            db.Orders.Add(MapOrder(order));
            foreach (var line in order.Lines)
                db.OrderLines.Add(MapOrderLine(order.Id, line));
        }

        await db.SaveChangesAsync(ct);

        _embeddings.Replace(embeddings);
    }

    private static Product MapProduct(StoreProductDto dto) => new()
    {
        Id = dto.Id,
        Sku = dto.Sku,
        Name = dto.Name,
        Description = dto.Description,
        PriceAmount = dto.PriceAmount,
        PriceCurrency = dto.PriceCurrency,
        CategoryId = dto.CategoryId,
        IsActive = dto.IsActive,
        CreatedAt = dto.CreatedAt,
    };

    private static Order MapOrder(StoreOrderDto dto) => new()
    {
        Id = dto.Id,
        OrderNumber = dto.OrderNumber,
        CustomerId = dto.CustomerId,
        Status = ResolveOrderStatus(dto.Status),
        Currency = dto.Currency,
        CreatedAt = dto.CreatedAt,
    };

    private static OrderLine MapOrderLine(Guid orderId, StoreOrderLineDto dto) => new()
    {
        Id = dto.Id,
        OrderId = orderId,
        ProductId = dto.ProductId,
        ProductName = dto.ProductName,
        Quantity = dto.Quantity,
    };

    private static string BuildEmbeddingText(StoreProductDto dto)
        => string.Join(' ', new[] { dto.Name, dto.Description }.Where(s => !string.IsNullOrWhiteSpace(s)));

    private static int ResolveOrderStatus(string status) => status switch
    {
        "Draft" => 1,
        "Pending" => 2,
        "Paid" => 3,
        "Shipped" => 4,
        "Delivered" => 5,
        "Cancelled" => RecommenderDbContext.CancelledOrderStatus,
        // Status necunoscut: tratat ca Pending (nu afectează filtrarea comenzilor anulate).
        _ => 2,
    };
}
