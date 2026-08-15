namespace StoreApi.Domain.Entities;

public sealed class Review
{
    private Review()
    {
    }

    public Review(Guid productId, Guid customerId, int rating, string title, string? comment = null)
    {
        Id = Guid.NewGuid();

        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId is required.", nameof(productId));

        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId is required.", nameof(customerId));

        ProductId = productId;
        CustomerId = customerId;
        SetRating(rating);
        SetTitle(title);
        Comment = comment;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public Guid CustomerId { get; private set; }

    public int Rating { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Comment { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public Product Product { get; private set; } = null!;

    private void SetRating(int rating)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");

        Rating = rating;
    }

    private void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        Title = title.Trim();
    }
}
