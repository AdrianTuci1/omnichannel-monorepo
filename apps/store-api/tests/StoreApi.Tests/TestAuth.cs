using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StoreApi.Api;

namespace StoreApi.Tests;

/// <summary>
/// Helper care înregistrează + autentifică un utilizator și returnează un HttpClient
/// cu header-ul `Authorization: Bearer ...` setat.
/// </summary>
public static class TestAuth
{
    public const string Password = "S3cure!Passw0rd";

    public static async Task<HttpClient> CreateAuthenticatedClientAsync(WebApplicationFactory<global::Program> factory)
    {
        var client = factory.CreateClient();
        var email = $"auth-{Guid.NewGuid():N}@example.com";

        var register = await client.PostAsJsonAsync("/auth/register", new
        {
            Email = email,
            Password = Password,
            FirstName = "Test",
            LastName = "User",
        });

        if (register.StatusCode != HttpStatusCode.Created)
            throw new InvalidOperationException($"Register failed ({register.StatusCode}): {await register.Content.ReadAsStringAsync()}");

        var login = await client.PostAsJsonAsync("/auth/login", new
        {
            Email = email,
            Password = Password,
        });

        if (login.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException($"Login failed ({login.StatusCode}): {await login.Content.ReadAsStringAsync()}");

        var token = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }
}
