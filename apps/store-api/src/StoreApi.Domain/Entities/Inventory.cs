namespace StoreApi.Domain.Entities;

public sealed class Inventory
{
    private Inventory()
    {
    }

    public Inventory(Guid productId, int quantityOnHand, int reorderThreshold = 0)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId is required.", nameof(productId));

        if (quantityOnHand < 0)
            throw new ArgumentOutOfRangeException(nameof(quantityOnHand));

        if (reorderThreshold < 0)
            throw new ArgumentOutOfRangeException(nameof(reorderThreshold));

        ProductId = productId;
        QuantityOnHand = quantityOnHand;
        ReorderThreshold = reorderThreshold;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public int QuantityOnHand { get; private set; }

    public int Reserved { get; private set; }

    public int ReorderThreshold { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public int Available => QuantityOnHand - Reserved;

    public void SetLevels(int quantityOnHand, int reserved, int reorderThreshold)
    {
        if (quantityOnHand < 0)
            throw new ArgumentOutOfRangeException(nameof(quantityOnHand));

        if (reserved < 0)
            throw new ArgumentOutOfRangeException(nameof(reserved));

        if (reorderThreshold < 0)
            throw new ArgumentOutOfRangeException(nameof(reorderThreshold));

        QuantityOnHand = quantityOnHand;
        Reserved = reserved;
        ReorderThreshold = reorderThreshold;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Adjust(int delta)
    {
        var next = QuantityOnHand + delta;
        if (next < 0)
            throw new InvalidOperationException($"Insufficient stock: cannot go below zero (current {QuantityOnHand}, delta {delta}).");

        QuantityOnHand = next;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reserve(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (quantity > Available)
            throw new InvalidOperationException($"Insufficient available stock ({Available}) to reserve {quantity}.");

        Reserved += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Release(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        Reserved = Math.Max(0, Reserved - quantity);
        UpdatedAt = DateTime.UtcNow;
    }
}
