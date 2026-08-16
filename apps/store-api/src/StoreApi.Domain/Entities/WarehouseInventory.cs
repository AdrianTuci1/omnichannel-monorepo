namespace StoreApi.Domain.Entities;

/// <summary>
/// Stocul unui produs într-un depozit. <c>QuantityOnHand</c> este stocul liber
/// (disponibil pentru alocare); <c>Reserved</c> este stocul deja alocat comenzilor.
/// Alocarea first-fit scade <c>QuantityOnHand</c> și crește <c>Reserved</c>.
/// </summary>
public sealed class WarehouseInventory
{
    private WarehouseInventory()
    {
    }

    public WarehouseInventory(Guid warehouseId, Guid productId, int quantityOnHand)
    {
        if (warehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId is required.", nameof(warehouseId));

        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId is required.", nameof(productId));

        if (quantityOnHand < 0)
            throw new ArgumentOutOfRangeException(nameof(quantityOnHand));

        WarehouseId = warehouseId;
        ProductId = productId;
        QuantityOnHand = quantityOnHand;
        Reserved = 0;
    }

    public Guid WarehouseId { get; private set; }

    public Warehouse Warehouse { get; private set; } = null!;

    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public int QuantityOnHand { get; private set; }

    public int Reserved { get; private set; }

    public void SetLevels(int quantityOnHand, int reserved)
    {
        if (quantityOnHand < 0)
            throw new ArgumentOutOfRangeException(nameof(quantityOnHand));

        if (reserved < 0)
            throw new ArgumentOutOfRangeException(nameof(reserved));

        QuantityOnHand = quantityOnHand;
        Reserved = reserved;
    }

    /// <summary>
    /// Rezervă <paramref name="quantity"/> unități din stocul liber (first-fit):
    /// scade QuantityOnHand și crește Reserved.
    /// </summary>
    public void Allocate(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (quantity > QuantityOnHand)
            throw new InvalidOperationException(
                $"Insufficient free stock in warehouse {WarehouseId}: requested {quantity}, available {QuantityOnHand}.");

        QuantityOnHand -= quantity;
        Reserved += quantity;
    }
}
