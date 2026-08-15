using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StoreApi.Api;
using Xunit;

namespace StoreApi.Tests;

public class UpdateEndpointsApiTests : IClassFixture<WebApplicationFactory<global::Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<global::Program> _factory;
    private HttpClient _client = null!;

    public UpdateEndpointsApiTests(WebApplicationFactory<global::Program> factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = await TestAuth.CreateAuthenticatedClientAsync(_factory);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Put_Category_Updates_Fields()
    {
        var postResponse = await _client.PostAsJsonAsync("/categories", new
        {
            Name = "Electronics",
            Slug = "electronics",
            Description = "old"
        });
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var created = await postResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.NotNull(created);

        var putResponse = await _client.PutAsJsonAsync($"/categories/{created!.Id}", new
        {
            Name = "Home Electronics",
            Slug = "home-electronics",
            Description = "new description"
        });

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var updated = await putResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Home Electronics", updated!.Name);
        Assert.Equal("home-electronics", updated.Slug);
        Assert.Equal("new description", updated.Description);
    }

    [Fact]
    public async Task Put_Category_With_Parent_And_Self_Parent_Rejected()
    {
        var postResponse = await _client.PostAsJsonAsync("/categories", new
        {
            Name = "Parentable",
            Slug = "parentable"
        });
        var created = await postResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var selfParent = await _client.PutAsJsonAsync($"/categories/{created!.Id}", new
        {
            Name = "Parentable",
            Slug = "parentable",
            ParentId = created.Id
        });
        Assert.Equal(HttpStatusCode.BadRequest, selfParent.StatusCode);
    }

    [Fact]
    public async Task Put_Customer_Updates_Fields()
    {
        var postResponse = await _client.PostAsJsonAsync("/customers", new
        {
            Email = $"upd-{Guid.NewGuid():N}@example.com",
            FirstName = "Old",
            LastName = "Name"
        });
        var created = await postResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(created);

        var putResponse = await _client.PutAsJsonAsync($"/customers/{created!.Id}", new
        {
            Email = $"upd2-{Guid.NewGuid():N}@example.com",
            FirstName = "New",
            LastName = "Name",
            Phone = "+40700123456"
        });

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var updated = await putResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(updated);
        Assert.Equal("New", updated!.FirstName);
        Assert.Equal("Name", updated.LastName);
        Assert.Equal("+40700123456", updated.Phone);
    }

    [Fact]
    public async Task Put_Order_Updates_Status()
    {
        var customer = await CreateCustomerAsync();
        var product = await CreateProductAsync();

        var postResponse = await _client.PostAsJsonAsync("/orders", new
        {
            CustomerId = customer.Id,
            Currency = "USD",
            Lines = new[] { new { ProductId = product.Id, Quantity = 1 } }
        });
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var created = await postResponse.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(created);
        Assert.Equal("Draft", created!.Status);

        var putResponse = await _client.PutAsJsonAsync($"/orders/{created.Id}", new { Status = "Paid" });
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var updated = await putResponse.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Paid", updated!.Status);
    }

    [Fact]
    public async Task Put_Order_With_Invalid_Status_Returns_BadRequest()
    {
        var customer = await CreateCustomerAsync();
        var product = await CreateProductAsync();

        var postResponse = await _client.PostAsJsonAsync("/orders", new
        {
            CustomerId = customer.Id,
            Lines = new[] { new { ProductId = product.Id, Quantity = 1 } }
        });
        var created = await postResponse.Content.ReadFromJsonAsync<OrderResponse>();

        var putResponse = await _client.PutAsJsonAsync($"/orders/{created!.Id}", new { Status = "NotAStatus" });
        Assert.Equal(HttpStatusCode.BadRequest, putResponse.StatusCode);
    }

    private async Task<CustomerResponse> CreateCustomerAsync()
    {
        var response = await _client.PostAsJsonAsync("/customers", new
        {
            Email = $"ord-{Guid.NewGuid():N}@example.com",
            FirstName = "Order",
            LastName = "User"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(customer);
        return customer!;
    }

    private async Task<ProductResponse> CreateProductAsync()
    {
        var sku = $"ORD-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        var response = await _client.PostAsJsonAsync("/products", new
        {
            Sku = sku,
            Name = "Order Product",
            PriceAmount = 20.00m,
            PriceCurrency = "USD"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(product);
        return product!;
    }
}
