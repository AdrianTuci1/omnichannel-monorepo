using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StoreApi.Api;
using Xunit;

namespace StoreApi.Tests;

public class WarehousePaymentApiTests : IClassFixture<WebApplicationFactory<global::Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<global::Program> _factory;
    private HttpClient _client = null!;

    public WarehousePaymentApiTests(WebApplicationFactory<global::Program> factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = await TestAuth.CreateAuthenticatedClientAsync(_factory);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Create_Order_With_Card_PaymentMethod()
    {
        var customer = await CreateCustomerAsync();
        var product = await CreateProductAsync("Card Payment Product", 25.00m);

        var response = await _client.PostAsJsonAsync("/orders", new
        {
            CustomerId = customer.Id,
            Currency = "USD",
            PaymentMethod = "Card",
            Lines = new[] { new { ProductId = product.Id, Quantity = 2 } }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.Equal("Card", order!.PaymentMethod);
        Assert.Equal("Pending", order.PaymentStatus);
        Assert.Equal(50.00m, order.TotalAmount);
    }

    [Fact]
    public async Task Create_Order_Defaults_To_CashOnDelivery()
    {
        var customer = await CreateCustomerAsync();
        var product = await CreateProductAsync("Cod Payment Product", 10.00m);

        var response = await _client.PostAsJsonAsync("/orders", new
        {
            CustomerId = customer.Id,
            Lines = new[] { new { ProductId = product.Id, Quantity = 1 } }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.Equal("CashOnDelivery", order!.PaymentMethod);
        Assert.Equal("Pending", order.PaymentStatus);
    }

    [Fact]
    public async Task Create_Order_With_Invalid_PaymentMethod_Returns_BadRequest()
    {
        var customer = await CreateCustomerAsync();
        var product = await CreateProductAsync("Invalid Payment Product", 10.00m);

        var response = await _client.PostAsJsonAsync("/orders", new
        {
            CustomerId = customer.Id,
            PaymentMethod = "Bitcoin",
            Lines = new[] { new { ProductId = product.Id, Quantity = 1 } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Warehouse_Then_List()
    {
        var code = $"WH-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        var create = await _client.PostAsJsonAsync("/warehouses", new
        {
            Name = "Main Warehouse",
            Code = code
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var warehouse = await create.Content.ReadFromJsonAsync<WarehouseResponse>();
        Assert.NotNull(warehouse);
        Assert.Equal("Main Warehouse", warehouse!.Name);
        Assert.Equal(code, warehouse.Code);
        Assert.True(warehouse.IsActive);

        var list = await _client.GetAsync("/warehouses");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var warehouses = await list.Content.ReadFromJsonAsync<List<WarehouseResponse>>();
        Assert.NotNull(warehouses);
        Assert.Contains(warehouses!, w => w.Id == warehouse.Id);
    }

    [Fact]
    public async Task Order_Placement_Allocates_Stock_From_Warehouse()
    {
        var warehouse = await CreateWarehouseAsync();
        var product = await CreateProductAsync("Allocated Product", 10.00m);
        var customer = await CreateCustomerAsync();

        var setStock = await _client.PutAsJsonAsync($"/warehouses/{warehouse.Id}/inventory/{product.Id}", new
        {
            QuantityOnHand = 10,
            Reserved = 0
        });
        Assert.Equal(HttpStatusCode.OK, setStock.StatusCode);

        var orderResponse = await _client.PostAsJsonAsync("/orders", new
        {
            CustomerId = customer.Id,
            Lines = new[] { new { ProductId = product.Id, Quantity = 3 } }
        });
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);

        // Depozit: stoc liber scăzut, rezervat crescut (first-fit).
        var inventory = await _client.GetAsync($"/warehouses/{warehouse.Id}/inventory");
        Assert.Equal(HttpStatusCode.OK, inventory.StatusCode);
        var rows = await inventory.Content.ReadFromJsonAsync<List<WarehouseInventoryResponse>>();
        Assert.NotNull(rows);
        var row = Assert.Single(rows!);
        Assert.Equal(product.Id, row.ProductId);
        Assert.Equal(7, row.QuantityOnHand);
        Assert.Equal(3, row.Reserved);

        // Inventarul agregat (per produs) rămâne sincronizat cu depozitele.
        var aggregate = await _client.GetAsync($"/products/{product.Id}/inventory");
        Assert.Equal(HttpStatusCode.OK, aggregate.StatusCode);
        var agg = await aggregate.Content.ReadFromJsonAsync<InventoryResponse>();
        Assert.NotNull(agg);
        Assert.Equal(7, agg!.QuantityOnHand);
        Assert.Equal(3, agg.Reserved);
    }

    private async Task<WarehouseResponse> CreateWarehouseAsync()
    {
        var code = $"WH-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        var response = await _client.PostAsJsonAsync("/warehouses", new
        {
            Name = "Test Warehouse",
            Code = code
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var warehouse = await response.Content.ReadFromJsonAsync<WarehouseResponse>();
        Assert.NotNull(warehouse);
        return warehouse!;
    }

    private async Task<ProductResponse> CreateProductAsync(string name, decimal price)
    {
        var sku = $"WHP-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        var response = await _client.PostAsJsonAsync("/products", new
        {
            Sku = sku,
            Name = name,
            PriceAmount = price,
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
            Email = $"whp-{Guid.NewGuid():N}@example.com",
            FirstName = "Warehouse",
            LastName = "Test"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(customer);
        return customer!;
    }
}
