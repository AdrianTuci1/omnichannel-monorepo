using System.Net.Http.Json;
using System.Text.Json;
using Cdp.Worker.Configuration;
using Microsoft.Extensions.Options;

namespace Cdp.Worker.Clients;

/// <summary>
/// Client HTTP pentru outbox-ul de evenimente expus de store-api
/// (<c>GET /events?since=...</c>). Returnează evenimentele brute (JsonElement)
/// pentru a nu depinde de forma exactă a payload-ului.
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
        _http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
    }

    /// <summary>
    /// Preia evenimentele din outbox produse strict după <paramref name="since"/>.
    /// </summary>
    public async Task<IReadOnlyList<JsonElement>> GetEventsAsync(DateTimeOffset since, CancellationToken ct)
    {
        var query = Uri.EscapeDataString(since.ToString("O"));
        using var response = await _http.GetAsync($"events?since={query}", ct);
        await EnsureSuccessAsync(response, ct);

        return await response.Content.ReadFromJsonAsync<List<JsonElement>>(_json, ct)
            ?? new List<JsonElement>();
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException(
            $"store-api a returnat {(int)response.StatusCode} ({response.ReasonPhrase}): {body}");
    }
}
