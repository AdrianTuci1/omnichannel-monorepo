namespace StoreApi.Domain.Entities;

public sealed class Customer
{
    private Customer()
    {
    }

    public Customer(string email, string firstName, string lastName, string? phone = null)
    {
        Id = Guid.NewGuid();
        SetEmail(email);
        SetName(firstName, lastName);
        Phone = phone;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = null!;

    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    public string? Phone { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public ICollection<Order> Orders { get; private set; } = new List<Order>();

    public void UpdateName(string firstName, string lastName) => SetName(firstName, lastName);

    public void UpdateEmail(string email) => SetEmail(email);

    public void Update(string email, string firstName, string lastName, string? phone)
    {
        SetEmail(email);
        SetName(firstName, lastName);
        Phone = phone;
    }

    private void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("A valid email is required.", nameof(email));

        Email = email.Trim().ToLowerInvariant();
    }

    private void SetName(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }
}
