using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StoreApi.Api;
using Xunit;

namespace StoreApi.Tests;

public class AuthApiTests : IClassFixture<WebApplicationFactory<global::Program>>
{
    private readonly HttpClient _client;

    public AuthApiTests(WebApplicationFactory<global::Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_Returns_Created_With_UserId()
    {
        var email = $"reg-{Guid.NewGuid():N}@example.com";
        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            Email = email,
            Password = TestAuth.Password,
            FirstName = "Jane",
            LastName = "Doe",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.UserId);
    }

    [Fact]
    public async Task Register_Duplicate_Email_Returns_Conflict()
    {
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        var payload = new { Email = email, Password = TestAuth.Password, FirstName = "A", LastName = "B" };

        var first = await _client.PostAsJsonAsync("/auth/register", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/auth/register", payload);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_With_Invalid_Email_Returns_BadRequest()
    {
        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            Email = "not-an-email",
            Password = TestAuth.Password,
            FirstName = "A",
            LastName = "B",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Returns_Unauthorized()
    {
        var email = $"bad-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/auth/register", new
        {
            Email = email,
            Password = TestAuth.Password,
            FirstName = "A",
            LastName = "B",
        });

        var response = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = "wrong-password" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_Returns_Tokens_And_Refresh_Rotates()
    {
        var email = $"tok-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/auth/register", new
        {
            Email = email,
            Password = TestAuth.Password,
            FirstName = "A",
            LastName = "B",
        });

        var login = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = TestAuth.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var tokens = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(tokens);
        Assert.False(string.IsNullOrEmpty(tokens!.AccessToken));
        Assert.False(string.IsNullOrEmpty(tokens.RefreshToken));
        Assert.Equal(900, tokens.ExpiresIn);

        var refresh = await _client.PostAsJsonAsync("/auth/refresh", new { RefreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);

        var rotated = await refresh.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(rotated);
        Assert.False(string.IsNullOrEmpty(rotated!.AccessToken));
        Assert.NotEqual(tokens.RefreshToken, rotated.RefreshToken);

        // vechiul refresh token este invalid după rotire
        var reuse = await _client.PostAsJsonAsync("/auth/refresh", new { RefreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    [Fact]
    public async Task Logout_Invalidates_Refresh_Token()
    {
        var email = $"out-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/auth/register", new
        {
            Email = email,
            Password = TestAuth.Password,
            FirstName = "A",
            LastName = "B",
        });

        var login = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = TestAuth.Password });
        var tokens = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var logout = await _client.PostAsJsonAsync("/auth/logout", new { RefreshToken = tokens!.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refresh = await _client.PostAsJsonAsync("/auth/refresh", new { RefreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Mutating_Endpoint_Without_Token_Returns_Unauthorized()
    {
        var response = await _client.PostAsJsonAsync("/products", new
        {
            Sku = "AUTH-401",
            Name = "No auth",
            PriceAmount = 1.0m,
            PriceCurrency = "USD",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record RegisterResponse(Guid UserId);
}
