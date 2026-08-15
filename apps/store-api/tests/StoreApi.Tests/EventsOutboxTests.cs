using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StoreApi.Api;
using Xunit;

namespace StoreApi.Tests;

public class EventsOutboxTests : IClassFixture<WebApplicationFactory<global::Program>>
{
    private readonly HttpClient _client;

    public EventsOutboxTests(WebApplicationFactory<global::Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_Event_Writes_To_Outbox_As_Unprocessed()
    {
        var postResponse = await _client.PostAsJsonAsync("/events", new
        {
            Type = "TestEvent",
            Payload = "{\"x\":1}"
        });

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var created = await postResponse.Content.ReadFromJsonAsync<EventResponse>();
        Assert.NotNull(created);
        Assert.Equal("TestEvent", created!.Type);
        Assert.Null(created.ProcessedAt);

        var getResponse = await _client.GetAsync("/events");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var events = await getResponse.Content.ReadFromJsonAsync<List<EventResponse>>();
        Assert.NotNull(events);
        Assert.Contains(events!, e => e.Id == created.Id && e.ProcessedAt == null);
    }

    [Fact]
    public async Task Post_Product_Emits_ProductCreated_Event()
    {
        var sku = $"EVT-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        var postResponse = await _client.PostAsJsonAsync("/products", new
        {
            Sku = sku,
            Name = "Event Product",
            PriceAmount = 9.99m,
            PriceCurrency = "USD"
        });
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var product = await postResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(product);

        var getResponse = await _client.GetAsync("/events");
        var events = await getResponse.Content.ReadFromJsonAsync<List<EventResponse>>();
        Assert.NotNull(events);
        Assert.Contains(events!, e => e.Type == "ProductCreated" && e.Payload.Contains(product!.Id.ToString()));
    }

    [Fact]
    public async Task Put_Product_Emits_ProductUpdated_Event()
    {
        var sku = $"EVU-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        var postResponse = await _client.PostAsJsonAsync("/products", new
        {
            Sku = sku,
            Name = "Event Update Product",
            PriceAmount = 9.99m,
            PriceCurrency = "USD"
        });
        var product = await postResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(product);

        var putResponse = await _client.PutAsJsonAsync($"/products/{product!.Id}", new
        {
            Name = "Event Update Product Renamed",
            PriceAmount = 12.00m,
            PriceCurrency = "USD",
            CategoryId = product.CategoryId
        });
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var getResponse = await _client.GetAsync("/events");
        var events = await getResponse.Content.ReadFromJsonAsync<List<EventResponse>>();
        Assert.NotNull(events);
        Assert.Contains(events!, e => e.Type == "ProductUpdated" && e.Payload.Contains(product.Id.ToString()));
    }

    [Fact]
    public async Task Post_Order_Emits_OrderCreated_Event()
    {
        var customerResponse = await _client.PostAsJsonAsync("/customers", new
        {
            Email = $"evt-{Guid.NewGuid():N}@example.com",
            FirstName = "Event",
            LastName = "Orderer"
        });
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(customer);

        var productResponse = await _client.PostAsJsonAsync("/products", new
        {
            Sku = $"EVO-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            Name = "Event Order Product",
            PriceAmount = 5.00m,
            PriceCurrency = "USD"
        });
        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(product);

        var orderResponse = await _client.PostAsJsonAsync("/orders", new
        {
            CustomerId = customer!.Id,
            Lines = new[] { new { ProductId = product!.Id, Quantity = 1 } }
        });
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);

        var getResponse = await _client.GetAsync("/events");
        var events = await getResponse.Content.ReadFromJsonAsync<List<EventResponse>>();
        Assert.NotNull(events);
        Assert.Contains(events!, e => e.Type == "OrderCreated" && e.Payload.Contains(order!.Id.ToString()));
    }

    [Fact]
    public async Task Get_Events_With_Future_Since_Returns_Empty()
    {
        var getResponse = await _client.GetAsync("/events?since=9999-12-31T00:00:00Z");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var events = await getResponse.Content.ReadFromJsonAsync<List<EventResponse>>();
        Assert.NotNull(events);
        Assert.Empty(events);
    }
}
