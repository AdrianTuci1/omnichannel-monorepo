using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StoreApi.Api;
using Xunit;

namespace StoreApi.Tests;

public class ReviewsApiTests : IClassFixture<WebApplicationFactory<global::Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<global::Program> _factory;
    private HttpClient _client = null!;

    public ReviewsApiTests(WebApplicationFactory<global::Program> factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = await TestAuth.CreateAuthenticatedClientAsync(_factory);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Post_Review_Then_Get_By_Product_And_Delete()
    {
        var product = await CreateProductAsync("Review Product");
        var customer = await CreateCustomerAsync();

        var postResponse = await _client.PostAsJsonAsync($"/products/{product.Id}/reviews", new
        {
            Rating = 5,
            Title = "Great product",
            Comment = "Loved it",
            CustomerId = customer.Id
        });

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var review = await postResponse.Content.ReadFromJsonAsync<ReviewResponse>();
        Assert.NotNull(review);
        Assert.Equal(5, review!.Rating);
        Assert.Equal("Great product", review.Title);
        Assert.Equal(product.Id, review.ProductId);
        Assert.Equal(customer.Id, review.CustomerId);

        var getResponse = await _client.GetAsync($"/products/{product.Id}/reviews");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var reviews = await getResponse.Content.ReadFromJsonAsync<List<ReviewResponse>>();
        Assert.NotNull(reviews);
        Assert.Contains(reviews!, r => r.Id == review.Id);

        var deleteResponse = await _client.DeleteAsync($"/reviews/{review.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDelete = await _client.GetAsync($"/products/{product.Id}/reviews");
        var reviewsAfter = await getAfterDelete.Content.ReadFromJsonAsync<List<ReviewResponse>>();
        Assert.NotNull(reviewsAfter);
        Assert.DoesNotContain(reviewsAfter!, r => r.Id == review.Id);
    }

    [Fact]
    public async Task Post_Review_With_Invalid_Rating_Returns_BadRequest()
    {
        var product = await CreateProductAsync("Invalid Rating Product");
        var customer = await CreateCustomerAsync();

        var response = await _client.PostAsJsonAsync($"/products/{product.Id}/reviews", new
        {
            Rating = 6,
            Title = "Out of range",
            CustomerId = customer.Id
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Review_For_Unknown_Product_Returns_NotFound()
    {
        var customer = await CreateCustomerAsync();

        var response = await _client.PostAsJsonAsync($"/products/{Guid.NewGuid()}/reviews", new
        {
            Rating = 4,
            Title = "Missing product",
            CustomerId = customer.Id
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<ProductResponse> CreateProductAsync(string name)
    {
        var sku = $"REV-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        var response = await _client.PostAsJsonAsync("/products", new
        {
            Sku = sku,
            Name = name,
            PriceAmount = 10.00m,
            PriceCurrency = "USD"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(product);
        return product!;
    }

    private async Task<CustomerResponse> CreateCustomerAsync()
    {
        var response = await _client.PostAsJsonAsync("/customers", new
        {
            Email = $"rev-{Guid.NewGuid():N}@example.com",
            FirstName = "Review",
            LastName = "User"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(customer);
        return customer!;
    }
}
