using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using StoreApi.Api;
using Xunit;

namespace StoreApi.Tests;

public class RelatedApiTests
{
    private static WebApplicationFactory<global::Program> CreateFactory()
        => new WebApplicationFactory<global::Program>()
            .WithWebHostBuilder(b => b.UseSetting("Recommender__BaseUrl", "http://127.0.0.1:1"));

    [Fact]
    public async Task Related_Returns_Empty_When_Recommender_Unavailable()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var sku = $"REL-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        var postResponse = await client.PostAsJsonAsync("/products", new
        {
            Sku = sku,
            Name = "Related Product",
            PriceAmount = 15.00m,
            PriceCurrency = "USD"
        });
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var product = await postResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(product);

        var relatedResponse = await client.GetAsync($"/products/{product!.Id}/related");
        Assert.Equal(HttpStatusCode.OK, relatedResponse.StatusCode);

        var items = await relatedResponse.Content.ReadFromJsonAsync<List<RelatedProductResponse>>();
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public async Task Related_Unknown_Product_Returns_NotFound()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var relatedResponse = await client.GetAsync($"/products/{Guid.NewGuid()}/related");
        Assert.Equal(HttpStatusCode.NotFound, relatedResponse.StatusCode);
    }
}
