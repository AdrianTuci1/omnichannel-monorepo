namespace OdooBridge.Models;

// DTO-uri oglindite după apps/store-api/src/StoreApi.Api/Contracts.cs.
// Serializarea folosește JsonSerializerDefaults.Web (camelCase), potrivit
// convenției ASP.NET Core folosite de store-api.

public sealed class StoreProduct
{
    public Guid Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal PriceAmount { get; set; }

    public string PriceCurrency { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class StoreCustomer
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class StoreOrder
{
    public Guid Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Currency { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public decimal TotalAmount { get; set; }

    public string TotalCurrency { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public IReadOnlyList<StoreOrderLine> Lines { get; set; } = Array.Empty<StoreOrderLine>();
}

public sealed class StoreOrderLine
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPriceAmount { get; set; }

    public string UnitPriceCurrency { get; set; } = string.Empty;

    public decimal LineTotalAmount { get; set; }

    public string LineTotalCurrency { get; set; } = string.Empty;
}

public sealed class CreateProductRequest
{
    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal PriceAmount { get; set; }

    public string PriceCurrency { get; set; } = "USD";

    public string? Description { get; set; }

    public Guid? CategoryId { get; set; }
}

public sealed class UpdateProductRequest
{
    public string Name { get; set; } = string.Empty;

    public decimal PriceAmount { get; set; }

    public string PriceCurrency { get; set; } = "USD";

    public Guid CategoryId { get; set; }

    public string? Description { get; set; }
}

public sealed class CreateCustomerRequest
{
    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }
}

public sealed class CreateOrderLineRequest
{
    public Guid ProductId { get; set; }

    public int Quantity { get; set; }
}

public sealed class CreateOrderRequest
{
    public Guid CustomerId { get; set; }

    public string? Currency { get; set; } = "USD";

    public string? Notes { get; set; }

    public IReadOnlyList<CreateOrderLineRequest>? Lines { get; set; }
}
