using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StoreApi.Api;
using Xunit;

namespace StoreApi.Tests;

public class ProductsApiTests : IClassFixture<WebApplicationFactory<global::Program>>
{
    private readonly HttpClient _client;

    public ProductsApiTests(WebApplicationFactory<global::Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_Returns_Ok()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_Product_Then_Get_By_Id_And_List()
    {
        var sku = $"TEST-{Guid.NewGuid():N}"[..16].ToUpperInvariant();

        var postResponse = await _client.PostAsJsonAsync("/products", new
        {
            Sku = sku,
            Name = "Test Widget",
            Description = "Integration test product",
            PriceAmount = 42.50m,
            PriceCurrency = "USD"
        });

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var created = await postResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(created);
        Assert.Equal(sku, created!.Sku);
        Assert.Equal(42.50m, created.PriceAmount);
        Assert.NotEqual(Guid.Empty, created.Id);

        var getResponse = await _client.GetAsync($"/products/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal("Test Widget", fetched.Name);

        var listResponse = await _client.GetAsync("/products");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var products = await listResponse.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.NotNull(products);
        Assert.Contains(products!, p => p.Sku == sku);
    }
}
