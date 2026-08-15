namespace StoreApi.Api;

public sealed record CreateCategoryRequest(
    string Name,
    string? Slug = null,
    string? Description = null,
    Guid? ParentId = null);

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid? ParentId);

public sealed record CreateCustomerRequest(
    string Email,
    string FirstName,
    string LastName,
    string? Phone = null);

public sealed record CustomerResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    DateTime CreatedAt);

public sealed record CreateProductRequest(
    string Sku,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    string? Description = null,
    Guid? CategoryId = null);

public sealed record UpdateProductRequest(
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    Guid CategoryId,
    string? Description = null);

public sealed record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal PriceAmount,
    string PriceCurrency,
    Guid CategoryId,
    bool IsActive,
    DateTime CreatedAt);

public sealed record CreateOrderLineRequest(
    Guid ProductId,
    int Quantity);

public sealed record CreateOrderRequest(
    Guid CustomerId,
    string? Currency = "USD",
    string? Notes = null,
    IReadOnlyList<CreateOrderLineRequest>? Lines = null);

public sealed record OrderLineResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPriceAmount,
    string UnitPriceCurrency,
    decimal LineTotalAmount,
    string LineTotalCurrency);

public sealed record OrderResponse(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string Status,
    string Currency,
    string? Notes,
    decimal TotalAmount,
    string TotalCurrency,
    DateTime CreatedAt,
    IReadOnlyList<OrderLineResponse> Lines);

public sealed record UpdateCategoryRequest(
    string Name,
    string? Slug = null,
    string? Description = null,
    Guid? ParentId = null);

public sealed record UpdateCustomerRequest(
    string Email,
    string FirstName,
    string LastName,
    string? Phone = null);

public sealed record UpdateOrderRequest(string Status);

public sealed record CreateReviewRequest(
    int Rating,
    string Title,
    Guid CustomerId,
    string? Comment = null);

public sealed record ReviewResponse(
    Guid Id,
    Guid ProductId,
    Guid CustomerId,
    int Rating,
    string Title,
    string? Comment,
    DateTime CreatedAt);

public sealed record UpdateInventoryRequest(
    int QuantityOnHand,
    int Reserved,
    int ReorderThreshold);

public sealed record InventoryResponse(
    Guid ProductId,
    int QuantityOnHand,
    int Reserved,
    int ReorderThreshold,
    int Available,
    DateTime UpdatedAt);

public sealed record RelatedProductResponse(
    Guid ProductId,
    string Name,
    double Score);

public sealed record CreateEventRequest(
    string Type,
    string Payload);

public sealed record EventResponse(
    Guid Id,
    string Type,
    string Payload,
    DateTime CreatedAt,
    DateTime? ProcessedAt);

public sealed record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName);

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn);

public sealed record AddCartItemRequest(
    Guid ProductId,
    int Quantity);

public sealed record UpdateCartItemRequest(int Quantity);

public sealed record CartItemResponse(
    Guid ProductId,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    int Quantity);
