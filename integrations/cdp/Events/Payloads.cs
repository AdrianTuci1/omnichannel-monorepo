namespace Cdp.Worker.Events;

/// <summary>
/// Proiecția payload-ului pentru evenimentele <c>customer.created</c> și <c>customer.updated</c>.
/// Câmpurile reflectă entitatea <c>Customer</c> din store-api (m1).
/// </summary>
public sealed class CustomerPayload
{
    public string? Email { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Phone { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }
}

/// <summary>
/// Proiecția payload-ului pentru evenimentele <c>order.*</c>.
/// Câmpurile reflectă entitatea <c>Order</c> din store-api (m1); <c>Status</c> este
/// numele enum-ului <c>OrderStatus</c> (Draft, Pending, Paid, Shipped, Delivered, Cancelled).
/// </summary>
public sealed class OrderPayload
{
    public string? OrderNumber { get; init; }

    public string? CustomerId { get; init; }

    public string? Status { get; init; }

    public string? Currency { get; init; }

    public decimal? TotalAmount { get; init; }

    public string? TotalCurrency { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }
}

/// <summary>
/// Proiecția payload-ului pentru evenimentele <c>product.*</c>.
/// Câmpurile reflectă entitatea <c>Product</c> din store-api (m1).
/// </summary>
public sealed class ProductPayload
{
    public string? Sku { get; init; }

    public string? Name { get; init; }

    public decimal? PriceAmount { get; init; }

    public string? PriceCurrency { get; init; }

    public string? CategoryId { get; init; }

    public bool? IsActive { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }
}
