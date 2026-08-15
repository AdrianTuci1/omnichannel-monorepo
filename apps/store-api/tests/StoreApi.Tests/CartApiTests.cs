using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StoreApi.Api;
using Xunit;

namespace StoreApi.Tests;

public class CartApiTests : IClassFixture<WebApplicationFactory<global::Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<global::Program> _factory;
    private HttpClient _client = null!;

    public CartApiTests(WebApplicationFactory<global::Program> factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = await TestAuth.CreateAuthenticatedClientAsync(_factory);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Cart_Add_Update_Delete()
    {
        var product = await CreateProductAsync("Cart Product", 12.50m);

        var empty = await _client.GetAsync("/cart");
        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        var emptyItems = await empty.Content.ReadFromJsonAsync<List<CartItemResponse>>();
        Assert.Empty(emptyItems!);

        var add = await _client.PostAsJsonAsync("/cart/items", new { ProductId = product.Id, Quantity = 2 });
        Assert.Equal(HttpStatusCode.OK, add.StatusCode);

        var items = await add.Content.ReadFromJsonAsync<List<CartItemResponse>>();
        Assert.NotNull(items);
        Assert.Single(items!);
        Assert.Equal(product.Id, items[0].ProductId);
        Assert.Equal("Cart Product", items[0].Name);
        Assert.Equal(12.50m, items[0].PriceAmount);
        Assert.Equal(2, items[0].Quantity);

        var update = await _client.PutAsJsonAsync($"/cart/items/{product.Id}", new { Quantity = 5 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<List<CartItemResponse>>();
        Assert.NotNull(updated);
        Assert.Single(updated!);
        Assert.Equal(5, updated[0].Quantity);

        var delete = await _client.DeleteAsync($"/cart/items/{product.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var after = await _client.GetAsync("/cart");
        var afterItems = await after.Content.ReadFromJsonAsync<List<CartItemResponse>>();
        Assert.Empty(afterItems!);
    }

    [Fact]
    public async Task Cart_Add_Same_Product_Increments_Quantity()
    {
        var product = await CreateProductAsync("Increment Product", 3.00m);

        await _client.PostAsJsonAsync("/cart/items", new { ProductId = product.Id, Quantity = 1 });
        var second = await _client.PostAsJsonAsync("/cart/items", new { ProductId = product.Id, Quantity = 4 });

        var items = await second.Content.ReadFromJsonAsync<List<CartItemResponse>>();
        Assert.NotNull(items);
        Assert.Single(items!);
        Assert.Equal(5, items[0].Quantity);
    }

    [Fact]
    public async Task Cart_Add_Unknown_Product_Returns_NotFound()
    {
        var add = await _client.PostAsJsonAsync("/cart/items", new { ProductId = Guid.NewGuid(), Quantity = 1 });
        Assert.Equal(HttpStatusCode.NotFound, add.StatusCode);
    }

    [Fact]
    public async Task Cart_Requires_Authentication()
    {
        using var anon = _factory.CreateClient();

        var get = await anon.GetAsync("/cart");
        Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);

        var post = await anon.PostAsJsonAsync("/cart/items", new { ProductId = Guid.NewGuid(), Quantity = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, post.StatusCode);
    }

    private async Task<ProductResponse> CreateProductAsync(string name, decimal price)
    {
        var response = await _client.PostAsJsonAsync("/products", new
        {
            Sku = $"CART-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            Name = name,
            PriceAmount = price,
            PriceCurrency = "USD",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(product);
        return product!;
    }
}
