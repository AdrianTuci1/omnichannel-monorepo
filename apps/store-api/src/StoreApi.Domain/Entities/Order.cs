using StoreApi.Domain.Common;

namespace StoreApi.Domain.Entities;

public sealed class Order
{
    private readonly List<OrderLine> _lines = new();

    private Order()
    {
    }

    public Order(Guid customerId, string currency = "USD", string? notes = null)
    {
        Id = Guid.NewGuid();
        OrderNumber = GenerateOrderNumber();
        CustomerId = customerId;
        Currency = NormalizeCurrency(currency);
        Notes = notes;
        Status = OrderStatus.Draft;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string OrderNumber { get; private set; } = null!;

    public Guid CustomerId { get; private set; }

    public Customer Customer { get; private set; } = null!;

    public OrderStatus Status { get; private set; }

    public string Currency { get; private set; } = "USD";

    public string? Notes { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public Money Total => _lines.Aggregate(new Money(0m, Currency), (sum, line) => sum.Add(line.LineTotal));

    public OrderLine AddLine(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        var line = new OrderLine(productId, productName, new Money(unitPrice, Currency), quantity);
        _lines.Add(line);
        Touch();
        return line;
    }

    public void RemoveLine(Guid lineId)
    {
        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new InvalidOperationException($"Order line {lineId} not found.");

        _lines.Remove(line);
        Touch();
    }

    public void ChangeLineQuantity(Guid lineId, int quantity)
    {
        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new InvalidOperationException($"Order line {lineId} not found.");

        line.ChangeQuantity(quantity);
        Touch();
    }

    public void SetStatus(OrderStatus status)
    {
        Status = status;
        Touch();
    }

    public void Submit()
    {
        EnsureStatus(OrderStatus.Draft, "submit");
        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot submit an empty order.");

        Status = OrderStatus.Pending;
        Touch();
    }

    public void MarkPaid()
    {
        EnsureStatus(OrderStatus.Pending, "mark paid");
        Status = OrderStatus.Paid;
        Touch();
    }

    public void MarkShipped()
    {
        EnsureStatus(OrderStatus.Paid, "mark shipped");
        Status = OrderStatus.Shipped;
        Touch();
    }

    public void MarkDelivered()
    {
        EnsureStatus(OrderStatus.Shipped, "mark delivered");
        Status = OrderStatus.Delivered;
        Touch();
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Delivered)
            throw new InvalidOperationException("A delivered order cannot be cancelled.");

        Status = OrderStatus.Cancelled;
        Touch();
    }

    private void EnsureStatus(OrderStatus expected, string action)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Cannot {action} an order in status {Status}; expected {expected}.");
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;

    private static string GenerateOrderNumber()
        => $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

    private static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        return currency.Trim().ToUpperInvariant();
    }
}
