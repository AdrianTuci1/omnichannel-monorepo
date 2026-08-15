using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StoreApi.Api;
using Xunit;

namespace StoreApi.Tests;

public class OrdersApiTests : IClassFixture<WebApplicationFactory<global::Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<global::Program> _factory;
    private HttpClient _client = null!;

    public OrdersApiTests(WebApplicationFactory<global::Program> factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = await TestAuth.CreateAuthenticatedClientAsync(_factory);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Post_Order_With_Line_Computes_Total()
    {
        var customerResponse = await _client.PostAsJsonAsync("/customers", new
        {
            Email = "jane@example.com",
            FirstName = "Jane",
            LastName = "Doe"
        });
        Assert.Equal(HttpStatusCode.Created, customerResponse.StatusCode);
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(customer);

        var productResponse = await _client.PostAsJsonAsync("/products", new
        {
            Sku = "ORD-SKU-0001",
            Name = "Order Product",
            PriceAmount = 25.00m,
            PriceCurrency = "USD"
        });
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);
        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(product);

        var orderResponse = await _client.PostAsJsonAsync("/orders", new
        {
            CustomerId = customer!.Id,
            Currency = "USD",
            Lines = new[] { new { ProductId = product!.Id, Quantity = 3 } }
        });
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);

        var order = await orderResponse.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.Equal(75.00m, order!.TotalAmount);
        Assert.Equal("USD", order.TotalCurrency);
        Assert.Single(order.Lines);

        var getResponse = await _client.GetAsync($"/orders/{order.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(fetched);
        Assert.Equal(order.Id, fetched!.Id);
        Assert.Equal(75.00m, fetched.TotalAmount);
        Assert.Single(fetched.Lines);
        Assert.Equal(3, fetched.Lines[0].Quantity);
        Assert.Equal("Order Product", fetched.Lines[0].ProductName);
    }
}
