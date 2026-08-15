using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StoreApi.Api;
using StoreApi.Domain.Common;
using StoreApi.Domain.Entities;
using StoreApi.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<StoreDbContext>(options =>
    options.UseInMemoryDatabase("StoreApi"));

// CORS: permite clienților browser (web, pos, dashboard) să consume API-ul
// dintr-un alt origin/port. API-ul nu are autentificare în acest stadiu, deci
// orice origin este permis.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddHttpClient();

var app = builder.Build();

app.UseCors();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
    db.Database.EnsureCreated();
    SeedDefaultCategory(db);
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// ---------- Categories ----------
app.MapGet("/categories", async (StoreDbContext db, CancellationToken ct) =>
{
    var categories = await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);
    return Results.Ok(categories.Select(ToCategoryResponse));
});

app.MapGet("/categories/{id:guid}", async (Guid id, StoreDbContext db, CancellationToken ct) =>
{
    var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
    return category is null ? Results.NotFound() : Results.Ok(ToCategoryResponse(category));
});

app.MapPost("/categories", async (CreateCategoryRequest request, StoreDbContext db, CancellationToken ct) =>
{
    var category = new Category(request.Name, request.Slug ?? string.Empty, request.Description, request.ParentId);
    db.Categories.Add(category);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/categories/{category.Id}", ToCategoryResponse(category));
});

app.MapPut("/categories/{id:guid}", async (Guid id, UpdateCategoryRequest request, StoreDbContext db, CancellationToken ct) =>
{
    var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
    if (category is null) return Results.NotFound();

    if (request.ParentId.HasValue)
    {
        if (request.ParentId.Value == id)
            return Results.BadRequest("A category cannot be its own parent.");

        var parentExists = await db.Categories.AnyAsync(c => c.Id == request.ParentId.Value, ct);
        if (!parentExists)
            return Results.BadRequest("Parent category not found.");
    }

    category.Update(request.Name, request.Slug, request.Description, request.ParentId);
    await db.SaveChangesAsync(ct);
    return Results.Ok(ToCategoryResponse(category));
});

app.MapDelete("/categories/{id:guid}", async (Guid id, StoreDbContext db, CancellationToken ct) =>
{
    var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
    if (category is null) return Results.NotFound();
    db.Categories.Remove(category);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});

// ---------- Customers ----------
app.MapGet("/customers", async (StoreDbContext db, CancellationToken ct) =>
{
    var customers = await db.Customers.AsNoTracking().OrderBy(c => c.Email).ToListAsync(ct);
    return Results.Ok(customers.Select(ToCustomerResponse));
});

app.MapGet("/customers/{id:guid}", async (Guid id, StoreDbContext db, CancellationToken ct) =>
{
    var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
    return customer is null ? Results.NotFound() : Results.Ok(ToCustomerResponse(customer));
});

app.MapPost("/customers", async (CreateCustomerRequest request, StoreDbContext db, CancellationToken ct) =>
{
    var customer = new Customer(request.Email, request.FirstName, request.LastName, request.Phone);
    db.Customers.Add(customer);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/customers/{customer.Id}", ToCustomerResponse(customer));
});

app.MapPut("/customers/{id:guid}", async (Guid id, UpdateCustomerRequest request, StoreDbContext db, CancellationToken ct) =>
{
    var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
    if (customer is null) return Results.NotFound();

    customer.Update(request.Email, request.FirstName, request.LastName, request.Phone);
    await db.SaveChangesAsync(ct);
    return Results.Ok(ToCustomerResponse(customer));
});

app.MapDelete("/customers/{id:guid}", async (Guid id, StoreDbContext db, CancellationToken ct) =>
{
    var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
    if (customer is null) return Results.NotFound();
    db.Customers.Remove(customer);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});

// ---------- Products ----------
app.MapGet("/products", async (StoreDbContext db, CancellationToken ct) =>
{
    var products = await db.Products.AsNoTracking()
        .Where(p => p.IsActive)
        .OrderBy(p => p.Name)
        .ToListAsync(ct);
    return Results.Ok(products.Select(ToProductResponse));
});

app.MapGet("/products/{id:guid}", async (Guid id, StoreDbContext db, CancellationToken ct) =>
{
    var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
    return product is null ? Results.NotFound() : Results.Ok(ToProductResponse(product));
});

app.MapPost("/products", async (CreateProductRequest request, StoreDbContext db, CancellationToken ct) =>
{
    var categoryId = request.CategoryId ?? await GetDefaultCategoryIdAsync(db, ct);
    if (categoryId == Guid.Empty) return Results.BadRequest("CategoryId is required and no default category exists.");
    var categoryExists = await db.Categories.AnyAsync(c => c.Id == categoryId, ct);
    if (!categoryExists) return Results.BadRequest("Category not found.");

    var product = new Product(request.Sku, request.Name, new Money(request.PriceAmount, request.PriceCurrency), categoryId, request.Description);
    db.Products.Add(product);
    AddEvent(db, "ProductCreated", new { productId = product.Id, sku = product.Sku, name = product.Name });
    await db.SaveChangesAsync(ct);
    return Results.Created($"/products/{product.Id}", ToProductResponse(product));
});

app.MapPut("/products/{id:guid}", async (Guid id, UpdateProductRequest request, StoreDbContext db, CancellationToken ct) =>
{
    var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
    if (product is null) return Results.NotFound();
    var categoryExists = await db.Categories.AnyAsync(c => c.Id == request.CategoryId, ct);
    if (!categoryExists) return Results.BadRequest("Category not found.");

    product.Update(request.Name, new Money(request.PriceAmount, request.PriceCurrency), request.CategoryId, request.Description);
    AddEvent(db, "ProductUpdated", new { productId = product.Id, sku = product.Sku, name = product.Name });
    await db.SaveChangesAsync(ct);
    return Results.Ok(ToProductResponse(product));
});

app.MapDelete("/products/{id:guid}", async (Guid id, StoreDbContext db, CancellationToken ct) =>
{
    var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
    if (product is null) return Results.NotFound();
    db.Products.Remove(product);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});

// ---------- Reviews ----------
app.MapGet("/products/{id:guid}/reviews", async (Guid id, StoreDbContext db, CancellationToken ct) =>
{
    var reviews = await db.Reviews.AsNoTracking()
        .Where(r => r.ProductId == id)
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync(ct);
    return Results.Ok(reviews.Select(ToReviewResponse));
});

app.MapPost("/products/{id:guid}/reviews", async (Guid id, CreateReviewRequest request, StoreDbContext db, CancellationToken ct) =>
{
    if (request.Rating < 1 || request.Rating > 5)
        return Results.BadRequest("Rating must be between 1 and 5.");

    var productExists = await db.Products.AnyAsync(p => p.Id == id, ct);
    if (!productExists) return Results.NotFound();

    var customerExists = await db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct);
    if (!customerExists) return Results.BadRequest("Customer not found.");

    var review = new Review(id, request.CustomerId, request.Rating, request.Title, request.Comment);
    db.Reviews.Add(review);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/reviews/{review.Id}", ToReviewResponse(review));
});

app.MapDelete("/reviews/{id:guid}", async (Guid id, StoreDbContext db, CancellationToken ct) =>
{
    var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == id, ct);
    if (review is null) return Results.NotFound();
    db.Reviews.Remove(review);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});

// ---------- Inventory ----------
app.MapGet("/products/{id:guid}/inventory", async (Guid id, StoreDbContext db, CancellationToken ct) =>
{
    var inventory = await db.Inventories.AsNoTracking().FirstOrDefaultAsync(i => i.ProductId == id, ct);
    return inventory is null ? Results.NotFound() : Results.Ok(ToInventoryResponse(inventory));
});

app.MapPut("/products/{id:guid}/inventory", async (Guid id, UpdateInventoryRequest request, StoreDbContext db, CancellationToken ct) =>
{
    if (request.QuantityOnHand < 0 || request.Reserved < 0 || request.ReorderThreshold < 0)
        return Results.BadRequest("QuantityOnHand, Reserved and ReorderThreshold must be non-negative.");

    var productExists = await db.Products.AnyAsync(p => p.Id == id, ct);
    if (!productExists) return Results.NotFound();

    var inventory = await db.Inventories.FirstOrDefaultAsync(i => i.ProductId == id, ct);
    if (inventory is null)
    {
        inventory = new Inventory(id, 0);
        db.Inventories.Add(inventory);
    }

    inventory.SetLevels(request.QuantityOnHand, request.Reserved, request.ReorderThreshold);
    await db.SaveChangesAsync(ct);
    return Results.Ok(ToInventoryResponse(inventory));
});

// ---------- Related products ----------
app.MapGet("/products/{id:guid}/related", async (Guid id, StoreDbContext db, IHttpClientFactory httpClientFactory, IConfiguration config, CancellationToken ct) =>
{
    if (!await db.Products.AnyAsync(p => p.Id == id, ct))
        return Results.NotFound();

    var baseUrl = config["Recommender__BaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
        baseUrl = "http://localhost:5181";

    var url = $"{baseUrl.TrimEnd('/')}/recommendations/{id}";

    try
    {
        using var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(3);
        using var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return Results.Ok(Enumerable.Empty<RelatedProductResponse>());

        var body = await response.Content.ReadFromJsonAsync<RecommenderResponseDto>(cancellationToken: ct);
        var items = body?.Items ?? Array.Empty<RecommenderItemDto>();
        return Results.Ok(items.Select(i => new RelatedProductResponse(i.ProductId, i.Name, i.Score)));
    }
    catch
    {
        // Recommender-ul indisponibil nu trebuie să crape endpoint-ul: returnăm o listă goală.
        return Results.Ok(Enumerable.Empty<RelatedProductResponse>());
    }
});

// ---------- Orders ----------
app.MapGet("/orders", async (StoreDbContext db, CancellationToken ct) =>
{
    var orders = await db.Orders.AsNoTracking()
        .Include(o => o.Lines)
        .OrderByDescending(o => o.CreatedAt)
        .ToListAsync(ct);
    return Results.Ok(orders.Select(ToOrderResponse));
});

app.MapGet("/orders/{id:guid}", async (Guid id, StoreDbContext db, CancellationToken ct) =>
{
    var order = await db.Orders.AsNoTracking()
        .Include(o => o.Lines)
        .FirstOrDefaultAsync(o => o.Id == id, ct);
    return order is null ? Results.NotFound() : Results.Ok(ToOrderResponse(order));
});

app.MapPost("/orders", async (CreateOrderRequest request, StoreDbContext db, CancellationToken ct) =>
{
    var customerExists = await db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct);
    if (!customerExists) return Results.BadRequest("Customer not found.");

    var order = new Order(request.CustomerId, request.Currency ?? "USD", request.Notes);

    if (request.Lines is not null)
    {
        foreach (var line in request.Lines)
        {
            var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == line.ProductId, ct);
            if (product is null) return Results.BadRequest($"Product {line.ProductId} not found.");
            order.AddLine(product.Id, product.Name, product.Price.Amount, line.Quantity);
        }
    }

    db.Orders.Add(order);
    AddEvent(db, "OrderCreated", new { orderId = order.Id, orderNumber = order.OrderNumber, customerId = order.CustomerId });
    await db.SaveChangesAsync(ct);
    return Results.Created($"/orders/{order.Id}", ToOrderResponse(order));
});

app.MapPut("/orders/{id:guid}", async (Guid id, UpdateOrderRequest request, StoreDbContext db, CancellationToken ct) =>
{
    var order = await db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, ct);
    if (order is null) return Results.NotFound();

    if (!Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var status))
        return Results.BadRequest($"Invalid status '{request.Status}'.");

    order.SetStatus(status);
    await db.SaveChangesAsync(ct);
    return Results.Ok(ToOrderResponse(order));
});

app.MapDelete("/orders/{id:guid}", async (Guid id, StoreDbContext db, CancellationToken ct) =>
{
    var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);
    if (order is null) return Results.NotFound();
    db.Orders.Remove(order);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});

// ---------- Events (outbox) ----------
app.MapPost("/events", async (CreateEventRequest request, StoreDbContext db, CancellationToken ct) =>
{
    var evt = new EventOutbox(request.Type, request.Payload);
    db.EventOutbox.Add(evt);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/events/{evt.Id}", ToEventResponse(evt));
});

app.MapGet("/events", async (DateTime? since, StoreDbContext db, CancellationToken ct) =>
{
    IQueryable<EventOutbox> query = db.EventOutbox.AsNoTracking().Where(e => e.ProcessedAt == null);
    if (since.HasValue)
        query = query.Where(e => e.CreatedAt > since.Value);

    var events = await query.OrderBy(e => e.CreatedAt).ToListAsync(ct);
    return Results.Ok(events.Select(ToEventResponse));
});

// ---------- mapping helpers ----------
CategoryResponse ToCategoryResponse(Category c) => new(c.Id, c.Name, c.Slug, c.Description, c.ParentId);

CustomerResponse ToCustomerResponse(Customer c) => new(c.Id, c.Email, c.FirstName, c.LastName, c.Phone, c.CreatedAt);

ProductResponse ToProductResponse(Product p) =>
    new(p.Id, p.Sku, p.Name, p.Description, p.Price.Amount, p.Price.Currency, p.CategoryId, p.IsActive, p.CreatedAt);

OrderLineResponse ToOrderLineResponse(OrderLine l) =>
    new(l.Id, l.ProductId, l.ProductName, l.Quantity, l.UnitPrice.Amount, l.UnitPrice.Currency, l.LineTotal.Amount, l.LineTotal.Currency);

OrderResponse ToOrderResponse(Order o) =>
    new(
        o.Id,
        o.OrderNumber,
        o.CustomerId,
        o.Status.ToString(),
        o.Currency,
        o.Notes,
        o.Total.Amount,
        o.Total.Currency,
        o.CreatedAt,
        o.Lines.Select(ToOrderLineResponse).ToList());

ReviewResponse ToReviewResponse(Review r) =>
    new(r.Id, r.ProductId, r.CustomerId, r.Rating, r.Title, r.Comment, r.CreatedAt);

InventoryResponse ToInventoryResponse(Inventory i) =>
    new(i.ProductId, i.QuantityOnHand, i.Reserved, i.ReorderThreshold, i.Available, i.UpdatedAt);

EventResponse ToEventResponse(EventOutbox e) =>
    new(e.Id, e.Type, e.Payload, e.CreatedAt, e.ProcessedAt);

void AddEvent(StoreDbContext db, string type, object payload)
{
    db.EventOutbox.Add(new EventOutbox(type, JsonSerializer.Serialize(payload)));
}

void SeedDefaultCategory(StoreDbContext db)
{
    if (db.Categories.Any()) return;
    db.Categories.Add(new Category("General", "general", "Default category"));
    db.SaveChanges();
}

async Task<Guid> GetDefaultCategoryIdAsync(StoreDbContext db, CancellationToken ct)
{
    var category = await db.Categories.AsNoTracking().OrderBy(c => c.Name).FirstOrDefaultAsync(ct);
    return category?.Id ?? Guid.Empty;
}

app.Run();

sealed record RecommenderItemDto(Guid ProductId, string Name, double Score);

sealed record RecommenderResponseDto(IReadOnlyList<RecommenderItemDto>? Items);

public partial class Program { }
