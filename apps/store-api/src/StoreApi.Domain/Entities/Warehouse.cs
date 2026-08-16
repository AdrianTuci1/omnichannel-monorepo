namespace StoreApi.Domain.Entities;

/// <summary>
/// Depozit fizic din care se alocă stocul la plasarea comenzilor.
/// </summary>
public sealed class Warehouse
{
    private Warehouse()
    {
    }

    public Warehouse(string name, string code)
    {
        Id = Guid.NewGuid();
        SetName(name);
        SetCode(code);
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public ICollection<WarehouseInventory> Inventory { get; private set; } = new List<WarehouseInventory>();

    public void Update(string name, string code, bool isActive)
    {
        SetName(name);
        SetCode(code);
        IsActive = isActive;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Name = name.Trim();
    }

    private void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));

        Code = code.Trim().ToUpperInvariant();
    }
}
