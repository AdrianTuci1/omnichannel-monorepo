namespace StoreApi.Domain.Entities;

/// <summary>
/// Utilizator autentificabil al API-ului. Parola este stocată doar ca hash BCrypt.
/// </summary>
public sealed class User
{
    private User()
    {
    }

    public User(string email, string passwordHash, string firstName, string lastName)
    {
        Id = Guid.NewGuid();
        Email = NormalizeEmail(email);
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        FirstName = NormalizeRequired(firstName, "First name");
        LastName = NormalizeRequired(lastName, "Last name");
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("A valid email is required.", nameof(email));

        return email.Trim().ToLowerInvariant();
    }

    private static string NormalizeRequired(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{field} is required.", nameof(value));

        return value.Trim();
    }
}
