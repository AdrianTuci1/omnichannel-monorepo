using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using AkeneoBridge.Configuration;
using AkeneoBridge.Models;

namespace AkeneoBridge.Clients;

/// <summary>
/// Client HTTP pentru API-ul REST al Akeneo PIM. Autentificare OAuth2 (password grant);
/// endpoint-ul și credențialele sunt configurabile prin <see cref="AkeneoOptions"/>.
/// </summary>
public sealed class AkeneoClient
{
    private readonly HttpClient _http;
    private readonly AkeneoOptions _options;
    private readonly JsonSerializerOptions _json;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private DateTime _tokenExpiresAt;

    public AkeneoClient(HttpClient http, AkeneoOptions options)
    {
        _http = http;
        _options = options;
        _json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
    }

    /// <summary>Enumeră toate produsele, urmând paginarea HATEOAS a API-ului.</summary>
    public IAsyncEnumerable<AkeneoProduct> GetAllProductsAsync()
        => EnumeratePagesAsync<AkeneoProduct>("api/rest/v1/products");

    /// <summary>Creează sau actualizează (upsert) un produs în Akeneo PIM.</summary>
    public async Task UpsertProductAsync(AkeneoProductWrite product, CancellationToken ct = default)
    {
        var uri = new Uri($"api/rest/v1/products/{Uri.EscapeDataString(product.Identifier)}", UriKind.Relative);
        await SendAuthenticatedAsync(HttpMethod.Patch, uri, product, ct);
    }

    /// <summary>Creează sau actualizează (upsert) un atribut în Akeneo PIM.</summary>
    public async Task UpsertAttributeAsync(AkeneoAttributeWrite attribute, CancellationToken ct = default)
    {
        var uri = new Uri($"api/rest/v1/attributes/{Uri.EscapeDataString(attribute.Code)}", UriKind.Relative);
        await SendAuthenticatedAsync(HttpMethod.Patch, uri, attribute, ct);
    }

    /// <summary>Enumeră toate atributele, urmând paginarea HATEOAS a API-ului.</summary>
    public IAsyncEnumerable<AkeneoAttribute> GetAllAttributesAsync()
        => EnumeratePagesAsync<AkeneoAttribute>("api/rest/v1/attributes");

    /// <summary>Enumeră toate categoriile, urmând paginarea HATEOAS a API-ului.</summary>
    public IAsyncEnumerable<AkeneoCategory> GetAllCategoriesAsync()
        => EnumeratePagesAsync<AkeneoCategory>("api/rest/v1/categories");

    private async IAsyncEnumerable<T> EnumeratePagesAsync<T>(
        string path,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var uri = new Uri($"{path}?page=1&limit={_options.PageSize}&with_count=false", UriKind.Relative);
        AkeneoPage<T>? page = await GetPageAsync<T>(uri, ct);
        while (page is not null)
        {
            foreach (var item in page.Embedded?.Items ?? Array.Empty<T>())
                yield return item;

            var next = page.Links?.Next?.Href;
            page = string.IsNullOrWhiteSpace(next)
                ? null
                : await GetPageAsync<T>(new Uri(next, UriKind.Absolute), ct);
        }
    }

    private async Task<AkeneoPage<T>> GetPageAsync<T>(Uri uri, CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(ct);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request, ct);
        return await ReadJsonAsync<AkeneoPage<T>>(response, ct);
    }

    /// <summary>Trimite o cerere autentificată (Bearer) cu corp JSON; nu așteaptă un corp de răspuns.</summary>
    private async Task SendAuthenticatedAsync(HttpMethod method, Uri uri, object body, CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(ct);
        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(body, body.GetType(), options: _json);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Akeneo API a returnat {(int)response.StatusCode} ({response.ReasonPhrase}): {errorBody}");
        }
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (_accessToken is not null && DateTime.UtcNow < _tokenExpiresAt)
            return _accessToken;

        await _tokenLock.WaitAsync(ct);
        try
        {
            if (_accessToken is not null && DateTime.UtcNow < _tokenExpiresAt)
                return _accessToken;

            var request = new TokenRequest(
                "password",
                _options.Username,
                _options.Password,
                _options.ClientId,
                _options.ClientSecret);

            using var response = await _http.PostAsJsonAsync(_options.OAuthTokenEndpoint, request, _json, ct);
            var token = await ReadJsonAsync<AkeneoTokenResponse>(response, ct);

            if (string.IsNullOrWhiteSpace(token.AccessToken))
                throw new InvalidOperationException("Akeneo nu a returnat un access token.");

            _accessToken = token.AccessToken;
            _tokenExpiresAt = DateTime.UtcNow.AddSeconds(Math.Max(token.ExpiresIn - 60, 0));
            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<T> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Akeneo API a returnat {(int)response.StatusCode} ({response.ReasonPhrase}): {body}");
        }

        return await response.Content.ReadFromJsonAsync<T>(_json, ct)
            ?? throw new InvalidOperationException("Răspuns gol de la Akeneo API.");
    }

    private sealed record TokenRequest(
        [property: JsonPropertyName("grant_type")] string GrantType,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("password")] string Password,
        [property: JsonPropertyName("client_id")] string ClientId,
        [property: JsonPropertyName("client_secret")] string ClientSecret);
}
