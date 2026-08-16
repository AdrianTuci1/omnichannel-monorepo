using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StoreApi.Api;
using Xunit;

namespace StoreApi.Tests;

public class FilteringApiTests : IClassFixture<WebApplicationFactory<global::Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<global::Program> _factory;
    private HttpClient _client = null!;

    public FilteringApiTests(WebApplicationFactory<global::Program> factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = await TestAuth.CreateAuthenticatedClientAsync(_factory);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Filter_By_Search_Returns_Matching_Products()
    {
        await CreateProductAsync("FilterZz Widget", 10.00m);
        await CreateProductAsync("FilterZz Gadget", 20.00m);

        var response = await _client.GetAsync("/products?search=Gadget");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ProductListResponse>();
        Assert.NotNull(result);
        Assert.Contains(result!.Items, p => p.Name == "FilterZz Gadget");
        Assert.DoesNotContain(result.Items, p => p.Name == "FilterZz Widget");
    }

    [Fact]
    public async Task Filter_By_Price_Range_Returns_Products_Within_Bounds()
    {
        await CreateProductAsync("RangeZz Cheap", 10.00m);
        await CreateProductAsync("RangeZz Mid", 50.00m);
        await CreateProductAsync("RangeZz Expensive", 100.00m);

        var response = await _client.GetAsync("/products?search=RangeZz&minPrice=20&maxPrice=80");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ProductListResponse>();
        Assert.NotNull(result);
        Assert.Contains(result!.Items, p => p.Name == "RangeZz Mid");
        Assert.DoesNotContain(result.Items, p => p.Name == "RangeZz Cheap");
        Assert.DoesNotContain(result.Items, p => p.Name == "RangeZz Expensive");
    }

    [Fact]
    public async Task Sort_By_Price_Ascending_Orders_Items()
    {
        await CreateProductAsync("SortZz Alpha", 30.00m);
        await CreateProductAsync("SortZz Beta", 10.00m);
        await CreateProductAsync("SortZz Gamma", 20.00m);

        var response = await _client.GetAsync("/products?search=SortZz&sort=price_asc");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ProductListResponse>();
        Assert.NotNull(result);
        var prices = result!.Items.Select(p => p.PriceAmount).ToList();

        Assert.Equal(new[] { 10.00m, 20.00m, 30.00m }, prices);
    }

    [Fact]
    public async Task Sort_By_Price_Descending_Orders_Items()
    {
        await CreateProductAsync("SortDescZz Alpha", 30.00m);
        await CreateProductAsync("SortDescZz Beta", 10.00m);
        await CreateProductAsync("SortDescZz Gamma", 20.00m);

        var response = await _client.GetAsync("/products?search=SortDescZz&sort=price_desc");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ProductListResponse>();
        Assert.NotNull(result);
        var prices = result!.Items.Select(p => p.PriceAmount).ToList();

        Assert.Equal(new[] { 30.00m, 20.00m, 10.00m }, prices);
    }

    [Fact]
    public async Task Pagination_Returns_Page_Total_And_Correct_Items()
    {
        await CreateProductAsync("PageZz Product 1", 1.00m);
        await CreateProductAsync("PageZz Product 2", 1.00m);
        await CreateProductAsync("PageZz Product 3", 1.00m);

        var page1 = await _client.GetAsync("/products?search=PageZz&page=1&pageSize=2");
        Assert.Equal(HttpStatusCode.OK, page1.StatusCode);
        var result1 = await page1.Content.ReadFromJsonAsync<ProductListResponse>();
        Assert.NotNull(result1);
        Assert.Equal(2, result1!.Items.Count);
        Assert.Equal(3, result1.Total);
        Assert.Equal(1, result1.Page);
        Assert.Equal(2, result1.PageSize);

        var page2 = await _client.GetAsync("/products?search=PageZz&page=2&pageSize=2");
        var result2 = await page2.Content.ReadFromJsonAsync<ProductListResponse>();
        Assert.NotNull(result2);
        Assert.Single(result2!.Items);
        Assert.Equal(3, result2.Total);
    }

    [Fact]
    public async Task InStock_Filter_Returns_Only_Stocked_Products()
    {
        var stocked = await CreateProductAsync("StockZz Stocked", 5.00m);
        await CreateProductAsync("StockZz Unstocked", 5.00m);

        var put = await _client.PutAsJsonAsync($"/products/{stocked.Id}/inventory", new
        {
            QuantityOnHand = 10,
            Reserved = 0,
            ReorderThreshold = 0
        });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var response = await _client.GetAsync("/products?search=StockZz&inStock=true");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ProductListResponse>();
        Assert.NotNull(result);
        Assert.Contains(result!.Items, p => p.Name == "StockZz Stocked");
        Assert.DoesNotContain(result.Items, p => p.Name == "StockZz Unstocked");
    }

    private async Task<ProductResponse> CreateProductAsync(string name, decimal price, string? description = null)
    {
        var sku = $"FLT-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        var response = await _client.PostAsJsonAsync("/products", new
        {
            Sku = sku,
            Name = name,
            Description = description,
            PriceAmount = price,
            PriceCurrency = "USD"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(product);
        return product!;
    }
}
