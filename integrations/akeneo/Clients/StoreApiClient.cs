using System.Net.Http.Json;
using System.Text.Json;
using AkeneoBridge.Configuration;
using AkeneoBridge.Models;

namespace AkeneoBridge.Clients;

/// <summary>
/// Client HTTP pentru backend-ul store-api. Folosește rutele expuse de apps/store-api
/// (CRUD /categories și /products) pentru a persista produsele sincronizate din Akeneo.
/// </summary>
public sealed class StoreApiClient
{
    private readonly HttpClient _http;
    private readonly StoreApiOptions _options;
    private readonly JsonSerializerOptions _json;

    public StoreApiClient(HttpClient http, StoreApiOptions options)
    {
        _http = http;
        _options = options;
        _json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task<IReadOnlyList<StoreCategoryResponse>> GetCategoriesAsync(CancellationToken ct)
    {
        using var response = await _http.GetAsync("categories", ct);
        return await ReadJsonAsync<IReadOnlyList<StoreCategoryResponse>>(response, ct)
            ?? Array.Empty<StoreCategoryResponse>();
    }

    public async Task<IReadOnlyList<StoreProductResponse>> GetProductsAsync(CancellationToken ct)
    {
        using var response = await _http.GetAsync("products", ct);
        return await ReadJsonAsync<IReadOnlyList<StoreProductResponse>>(response, ct)
            ?? Array.Empty<StoreProductResponse>();
    }

    /// <summary>Returnează produsele complete din store-api (pentru exportul invers către Akeneo).</summary>
    public async Task<IReadOnlyList<StoreProductFullResponse>> GetProductsFullAsync(CancellationToken ct)
    {
        using var response = await _http.GetAsync("products", ct);
        return await ReadJsonAsync<IReadOnlyList<StoreProductFullResponse>>(response, ct)
            ?? Array.Empty<StoreProductFullResponse>();
    }

    public async Task<Guid> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct)
    {
        using var response = await _http.PostAsJsonAsync("categories", request, _json, ct);
        var created = await ReadJsonAsync<StoreEntityResponse>(response, ct)
            ?? throw new InvalidOperationException("Răspuns gol de la Store API la crearea categoriei.");
        return created.Id;
    }

    public async Task<Guid> CreateProductAsync(CreateProductRequest request, CancellationToken ct)
    {
        using var response = await _http.PostAsJsonAsync("products", request, _json, ct);
        var created = await ReadJsonAsync<StoreEntityResponse>(response, ct)
            ?? throw new InvalidOperationException("Răspuns gol de la Store API la crearea produsului.");
        return created.Id;
    }

    public async Task UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken ct)
    {
        using var response = await _http.PutAsJsonAsync($"products/{id}", request, _json, ct);
        await ReadJsonAsync<StoreEntityResponse>(response, ct);
    }

    private async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Store API a returnat {(int)response.StatusCode} ({response.ReasonPhrase}): {body}");
        }

        return await response.Content.ReadFromJsonAsync<T>(_json, ct);
    }
}
