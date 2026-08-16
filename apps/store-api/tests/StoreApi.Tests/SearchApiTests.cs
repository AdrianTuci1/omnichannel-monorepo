using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StoreApi.Api;
using Xunit;

namespace StoreApi.Tests;

public class SearchApiTests : IClassFixture<WebApplicationFactory<global::Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<global::Program> _factory;
    private HttpClient _client = null!;

    public SearchApiTests(WebApplicationFactory<global::Program> factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = await TestAuth.CreateAuthenticatedClientAsync(_factory);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Search_By_Name_Returns_Matching_Products()
    {
        await CreateProductAsync("Searchable Widget", "SRCH-0001");
        await CreateProductAsync("Unrelated Gadget", "SRCH-0002");

        var response = await _client.GetAsync("/products/search?q=Widget");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content.ReadFromJsonAsync<ProductListResponse>();
        Assert.NotNull(products);
        Assert.Contains(products!.Items, p => p.Name == "Searchable Widget");
        Assert.DoesNotContain(products.Items, p => p.Name == "Unrelated Gadget");
    }

    [Fact]
    public async Task Search_By_Sku_Returns_Matching_Products()
    {
        await CreateProductAsync("Sku Widget", "SRCH-9999");

        var response = await _client.GetAsync("/products/search?q=SRCH-9999");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content.ReadFromJsonAsync<ProductListResponse>();
        Assert.NotNull(products);
        Assert.Contains(products!.Items, p => p.Sku == "SRCH-9999");
    }

    [Fact]
    public async Task Search_By_Description_Returns_Matching_Products()
    {
        await CreateProductAsync("Desc Widget", "SRCH-7777", "a very special description token");

        var response = await _client.GetAsync("/products/search?q=special%20description");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content.ReadFromJsonAsync<ProductListResponse>();
        Assert.NotNull(products);
        Assert.Contains(products!.Items, p => p.Sku == "SRCH-7777");
    }

    [Fact]
    public async Task Search_Empty_Query_Returns_All_Products()
    {
        await CreateProductAsync("Anything", "SRCH-5555");

        // `search` cu termen gol (alias peste /products) nu aplică filtru: returnează totul, paginat.
        var response = await _client.GetAsync("/products/search?q=");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content.ReadFromJsonAsync<ProductListResponse>();
        Assert.NotNull(products);
        Assert.True(products!.Total > 0);
        Assert.Contains(products.Items, p => p.Sku == "SRCH-5555");
    }

    private async Task<ProductResponse> CreateProductAsync(string name, string sku, string? description = null)
    {
        var response = await _client.PostAsJsonAsync("/products", new
        {
            Sku = sku,
            Name = name,
            Description = description,
            PriceAmount = 10.00m,
            PriceCurrency = "USD",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(product);
        return product!;
    }
}
