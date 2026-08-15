using System.Net.Http.Json;
using System.Text.Json;

namespace Recommender.Api.Clients;

/// <summary>Proiecție a răspunsului <c>GET /products</c> al Store API.</summary>
public sealed record StoreProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal PriceAmount,
    string PriceCurrency,
    Guid CategoryId,
    bool IsActive,
    DateTime CreatedAt);

/// <summary>Proiecție a răspunsului <c>GET /orders</c> al Store API (include liniile).</summary>
public sealed record StoreOrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string Status,
    string Currency,
    string? Notes,
    decimal TotalAmount,
    string TotalCurrency,
    DateTime CreatedAt,
    IReadOnlyList<StoreOrderLineDto> Lines);

/// <summary>Linie de comandă din răspunsul Store API.</summary>
public sealed record StoreOrderLineDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPriceAmount,
    string UnitPriceCurrency,
    decimal LineTotalAmount,
    string LineTotalCurrency);

/// <summary>
/// Client HTTP către Store API: citește catalogul de produse și istoricul de comenzi.
/// </summary>
public interface IStoreApiClient
{
    Task<IReadOnlyList<StoreProductDto>> GetProductsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<StoreOrderDto>> GetOrdersAsync(CancellationToken ct = default);
}

public sealed class StoreApiClient : IStoreApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public StoreApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<StoreProductDto>> GetProductsAsync(CancellationToken ct = default)
    {
        var products = await _http.GetFromJsonAsync<List<StoreProductDto>>("/products", JsonOptions, ct);
        return products ?? new List<StoreProductDto>();
    }

    public async Task<IReadOnlyList<StoreOrderDto>> GetOrdersAsync(CancellationToken ct = default)
    {
        var orders = await _http.GetFromJsonAsync<List<StoreOrderDto>>("/orders", JsonOptions, ct);
        return orders ?? new List<StoreOrderDto>();
    }
}
