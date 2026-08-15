using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OdooBridge.Configuration;
using OdooBridge.Models;

namespace OdooBridge.Clients;

/// <summary>
/// Client HTTP pentru store-api (apps/store-api). Endpoint-ul este configurabil
/// prin <see cref="StoreApiOptions.BaseUrl"/>.
/// </summary>
public sealed class StoreApiClient
{
    private readonly HttpClient _http;
    private readonly StoreApiOptions _options;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public StoreApiClient(HttpClient http, IOptions<StoreApiOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<StoreProduct>> GetProductsAsync(CancellationToken ct = default)
    {
        var products = await _http.GetFromJsonAsync<List<StoreProduct>>(BuildUrl("/products"), _json, ct);
        return products ?? new List<StoreProduct>();
    }

    public async Task<IReadOnlyList<StoreCustomer>> GetCustomersAsync(CancellationToken ct = default)
    {
        var customers = await _http.GetFromJsonAsync<List<StoreCustomer>>(BuildUrl("/customers"), _json, ct);
        return customers ?? new List<StoreCustomer>();
    }

    public async Task<IReadOnlyList<StoreOrder>> GetOrdersAsync(CancellationToken ct = default)
    {
        var orders = await _http.GetFromJsonAsync<List<StoreOrder>>(BuildUrl("/orders"), _json, ct);
        return orders ?? new List<StoreOrder>();
    }

    public async Task<StoreProduct> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync(BuildUrl("/products"), request, _json, ct);
        return await ReadProductAsync(response, ct);
    }

    public async Task<StoreProduct> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PutAsJsonAsync(BuildUrl($"/products/{id}"), request, _json, ct);
        return await ReadProductAsync(response, ct);
    }

    public async Task<StoreCustomer> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync(BuildUrl("/customers"), request, _json, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StoreCustomer>(_json, ct)
            ?? throw new InvalidOperationException("Răspuns gol la crearea clientului în store-api.");
    }

    public async Task<StoreOrder> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync(BuildUrl("/orders"), request, _json, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StoreOrder>(_json, ct)
            ?? throw new InvalidOperationException("Răspuns gol la crearea comenzii în store-api.");
    }

    private async Task<StoreProduct> ReadProductAsync(HttpResponseMessage response, CancellationToken ct)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StoreProduct>(_json, ct)
            ?? throw new InvalidOperationException("Răspuns gol la operația pe produs în store-api.");
    }

    private Uri BuildUrl(string path) => new(_options.BaseUrl.TrimEnd('/') + path);
}
