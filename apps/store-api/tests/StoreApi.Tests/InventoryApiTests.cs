using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StoreApi.Api;
using Xunit;

namespace StoreApi.Tests;

public class InventoryApiTests : IClassFixture<WebApplicationFactory<global::Program>>
{
    private readonly HttpClient _client;

    public InventoryApiTests(WebApplicationFactory<global::Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Put_Inventory_Then_Get()
    {
        var product = await CreateProductAsync("Inventory Product");

        var getBefore = await _client.GetAsync($"/products/{product.Id}/inventory");
        Assert.Equal(HttpStatusCode.NotFound, getBefore.StatusCode);

        var putResponse = await _client.PutAsJsonAsync($"/products/{product.Id}/inventory", new
        {
            QuantityOnHand = 100,
            Reserved = 10,
            ReorderThreshold = 20
        });

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var put = await putResponse.Content.ReadFromJsonAsync<InventoryResponse>();
        Assert.NotNull(put);
        Assert.Equal(100, put!.QuantityOnHand);
        Assert.Equal(10, put.Reserved);
        Assert.Equal(20, put.ReorderThreshold);
        Assert.Equal(90, put.Available);

        var getResponse = await _client.GetAsync($"/products/{product.Id}/inventory");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var get = await getResponse.Content.ReadFromJsonAsync<InventoryResponse>();
        Assert.NotNull(get);
        Assert.Equal(100, get!.QuantityOnHand);
        Assert.Equal(10, get.Reserved);
        Assert.Equal(90, get.Available);
    }

    [Fact]
    public async Task Put_Inventory_Updates_Existing_Record()
    {
        var product = await CreateProductAsync("Inventory Update Product");

        await _client.PutAsJsonAsync($"/products/{product.Id}/inventory", new
        {
            QuantityOnHand = 50,
            Reserved = 0,
            ReorderThreshold = 5
        });

        var putResponse = await _client.PutAsJsonAsync($"/products/{product.Id}/inventory", new
        {
            QuantityOnHand = 200,
            Reserved = 30,
            ReorderThreshold = 40
        });

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var put = await putResponse.Content.ReadFromJsonAsync<InventoryResponse>();
        Assert.NotNull(put);
        Assert.Equal(200, put!.QuantityOnHand);
        Assert.Equal(30, put.Reserved);
        Assert.Equal(170, put.Available);
    }

    [Fact]
    public async Task Put_Inventory_With_Negative_Value_Returns_BadRequest()
    {
        var product = await CreateProductAsync("Inventory Negative Product");

        var response = await _client.PutAsJsonAsync($"/products/{product.Id}/inventory", new
        {
            QuantityOnHand = -1,
            Reserved = 0,
            ReorderThreshold = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_Inventory_For_Unknown_Product_Returns_NotFound()
    {
        var response = await _client.PutAsJsonAsync($"/products/{Guid.NewGuid()}/inventory", new
        {
            QuantityOnHand = 10,
            Reserved = 0,
            ReorderThreshold = 0
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<ProductResponse> CreateProductAsync(string name)
    {
        var sku = $"INV-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        var response = await _client.PostAsJsonAsync("/products", new
        {
            Sku = sku,
            Name = name,
            PriceAmount = 5.00m,
            PriceCurrency = "USD"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(product);
        return product!;
    }
}
