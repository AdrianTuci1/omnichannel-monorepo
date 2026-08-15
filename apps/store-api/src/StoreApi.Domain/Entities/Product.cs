using StoreApi.Domain.Common;

namespace StoreApi.Domain.Entities;

public sealed class Product
{
    private Product()
    {
    }

    public Product(string sku, string name, Money price, Guid categoryId, string? description = null)
    {
        Id = Guid.NewGuid();
        SetSku(sku);
        SetName(name);
        Price = price ?? throw new ArgumentNullException(nameof(price));
        CategoryId = categoryId;
        Description = description;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public string Sku { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public Money Price { get; private set; } = null!;

    public Guid CategoryId { get; private set; }

    public Category Category { get; private set; } = null!;

    public ICollection<Review> Reviews { get; private set; } = new List<Review>();

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public void Update(string name, Money price, Guid categoryId, string? description)
    {
        SetName(name);
        Price = price ?? throw new ArgumentNullException(nameof(price));
        CategoryId = categoryId;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    private void SetSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU is required.", nameof(sku));

        Sku = sku.Trim().ToUpperInvariant();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Name = name.Trim();
    }
}
