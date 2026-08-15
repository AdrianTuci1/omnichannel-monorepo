namespace StoreApi.Domain.Entities;

public sealed class Category
{
    private Category()
    {
    }

    public Category(string name, string slug, string? description = null, Guid? parentId = null)
    {
        Id = Guid.NewGuid();
        SetName(name);
        Slug = string.IsNullOrWhiteSpace(slug) ? Slugify(name) : NormalizeSlug(slug);
        Description = description;
        ParentId = parentId;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string Slug { get; private set; } = null!;

    public string? Description { get; private set; }

    public Guid? ParentId { get; private set; }

    public Category? Parent { get; private set; }

    public ICollection<Category> Children { get; private set; } = new List<Category>();

    public ICollection<Product> Products { get; private set; } = new List<Product>();

    public void Rename(string name)
    {
        SetName(name);
        Slug = Slugify(name);
    }

    public void Update(string name, string? slug, string? description, Guid? parentId)
    {
        SetName(name);
        Slug = string.IsNullOrWhiteSpace(slug) ? Slugify(name) : NormalizeSlug(slug);
        Description = description;
        ParentId = parentId;
    }

    public void ChangeDescription(string? description) => Description = description;

    public static string Slugify(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        return string.Join('-', name.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Name = name.Trim();
    }

    private static string NormalizeSlug(string slug) => slug.Trim().ToLowerInvariant();
}
