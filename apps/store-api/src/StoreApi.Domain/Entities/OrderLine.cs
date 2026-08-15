using StoreApi.Domain.Common;

namespace StoreApi.Domain.Entities;

public sealed class OrderLine
{
    private OrderLine()
    {
    }

    public OrderLine(Guid productId, string productName, Money unitPrice, int quantity)
    {
        Id = Guid.NewGuid();

        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId is required.", nameof(productId));

        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name is required.", nameof(productName));

        ProductId = productId;
        ProductName = productName.Trim();
        UnitPrice = unitPrice ?? throw new ArgumentNullException(nameof(unitPrice));
        ChangeQuantity(quantity);
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Order Order { get; private set; } = null!;

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; } = null!;

    public Money UnitPrice { get; private set; } = null!;

    public int Quantity { get; private set; }

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    public void ChangeQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

        Quantity = quantity;
    }
}
