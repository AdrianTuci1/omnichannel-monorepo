using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Redis = StackExchange.Redis;
using StoreApi.Api;
using StoreApi.Api.Auth;
using StoreApi.Domain.Common;
using StoreApi.Domain.Entities;
using StoreApi.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ---------- Provider DB: InMemory local / PostgreSQL prod ----------
var connectionString = builder.Configuration.GetConnectionString("StoreApi");
var usePostgres = !string.IsNullOrWhiteSpace(connectionString);

builder.Services.AddDbContext<StoreDbContext>(options =>
{
    if (usePostgres)
    {
        options.UseNpgsql(connectionString!, npgsql => npgsql.UseVector());
    }
    else
    {
        options.UseInMemoryDatabase("StoreApi");
    }
});

// CORS: permite clienților browser (web, pos, dashboard) să consume API-ul
// dintr-un alt origin/port.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddHttpClient();

// ---------- Observabilitate: OpenTelemetry (traces HTTP + EF Core) ----------
// Exportul OTLP este opțional și configurabil prin appsettings (OpenTelemetry:OtlpEndpoint).
// Endpoint-ul Prometheus GET /metrics este expus separat (prometheus-net).
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
var otlpProtocol = builder.Configuration["OpenTelemetry:OtlpProtocol"];

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("store-api"));
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddEntityFrameworkCoreInstrumentation();

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
                if (string.Equals(otlpProtocol, "http", StringComparison.OrdinalIgnoreCase))
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
        }
    });

// ---------- Auth (JWT) ----------
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
    jwtSecret = "dev-only-secret-key-change-me-in-production-0123456789";

builder.Services.AddSingleton(new TokenService(jwtSecret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        };
    });

builder.Services.AddAuthorization();

// ---------- Redis (cache distribuit) cu fallback in-memory ----------
var redisConnection = builder.Configuration["Redis:ConnectionString"];
if (string.IsNullOrWhiteSpace(redisConnection))
    redisConnection = "localhost:6379";

Redis.IConnectionMultiplexer? redisMultiplexer = null;
try
{
    redisMultiplexer = Redis.ConnectionMultiplexer.Connect(new Redis.ConfigurationOptions
    {
        EndPoints = { redisConnection },
        AbortOnConnectFail = false,
        ConnectTimeout = 1500,
        ConnectRetry = 1,
    });
}
catch
{
    redisMultiplexer = null;
}

if (redisMultiplexer is not null && redisMultiplexer.IsConnected)
{
    builder.Services.AddSingleton<Redis.IConnectionMultiplexer>(redisMultiplexer);
    builder.Services.AddStackExchangeRedisCache(options =>
        options.ConnectionMultiplexerFactory = () => Task.FromResult(redisMultiplexer));
}
else
{
    redisMultiplexer?.Dispose();
    builder.Services.AddDistributedMemoryCache();
}

var app = builder.Build();

// ---------- Metrice Prometheus (http_requests_total, http_request_duration_seconds) ----------
var httpRequestsTotal = Metrics.CreateCounter("http_requests_total", "Total HTTP requests.", "code");
var httpRequestDurationSeconds = Metrics.CreateHistogram(
    "http_request_duration_seconds",
    "HTTP request duration in seconds.",
    new[] { "code" },
    new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(0.001, 2, 16) });

app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();
    try
    {
        await next();
    }
    finally
    {
        stopwatch.Stop();
        var code = context.Response.StatusCode.ToString();
        httpRequestsTotal.WithLabels(code).Inc();
        httpRequestDurationSeconds.WithLabels(code).Observe(stopwatch.Elapsed.TotalSeconds);
    }
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
    if (usePostgres)
    {
        db.Database.Migrate();
        SeedDefaultCategory(db);
    }
    else
    {
        db.Database.EnsureCreated();
        SeedDefaultCategory(db);
    }
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// ---------- Metrice Prometheus (text format) ----------
app.MapMetrics();

// ---------- Auth ----------
app.MapPost("/auth/register", async (RegisterRequest request, StoreDbContext db, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        return Results.BadRequest("A valid email is required.");
    if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        return Results.BadRequest("Password must be at least 6 characters.");
    if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        return Results.BadRequest("First name and last name are required.");

    var email = request.Email.Trim().ToLowerInvariant();
    if (await db.Users.AnyAsync(u => u.Email == email, ct))
        return Results.Conflict("Email is already registered.");

    var user = new User(email, BCrypt.Net.BCrypt.HashPassword(request.Password), request.FirstName, request.LastName);
    db.Users.Add(user);
    await db.SaveChangesAsync(ct);

    return Results.Created($"/users/{user.Id}", new { userId = user.Id });
});

app.MapPost("/auth/login", async (LoginRequest request, StoreDbContext db, IDistributedCache cache, TokenService tokens, CancellationToken ct) =>
{
    var email = request.Email?.Trim().ToLowerInvariant();
    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);
    if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password ?? string.Empty, user.PasswordHash))
        return Results.Unauthorized();

    var accessToken = tokens.CreateAccessToken(user.Id);
    var refreshToken = tokens.CreateRefreshToken();
    await cache.SetStringAsync($"refresh:{refreshToken}", user.Id.ToString(),
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TokenService.RefreshTokenLifetime }, ct);

    return Results.Ok(new LoginResponse(accessToken, refreshToken, (int)TokenService.AccessTokenLifetime.TotalSeconds));
});

app.MapPost("/auth/refresh", async (RefreshRequest request, IDistributedCache cache, TokenService tokens, CancellationToken ct) =>
{
    var refreshToken = request.RefreshToken?.Trim();
    if (string.IsNullOrEmpty(refreshToken))
        return Results.Unauthorized();

    var userIdString = await cache.GetStringAsync($"refresh:{refreshToken}", ct);
    if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        return Results.Unauthorized();

    // Rotire: invalidăm vechiul refresh token și emitem unul nou.
    await cache.RemoveAsync($"refresh:{refreshToken}", ct);

    var newRefreshToken = tokens.CreateRefreshToken();
    await cache.SetStringAsync($"refresh:{newRefreshToken}", userId.ToString(),
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TokenService.RefreshTokenLifetime }, ct);

    var accessToken = tokens.CreateAccessToken(userId);
    return Results.Ok(new LoginResponse(accessToken, newRefreshToken, (int)TokenService.AccessTokenLifetime.TotalSeconds));
});

app.MapPost("/auth/logout", async (RefreshRequest request, IDistributedCache cache, CancellationToken ct) =>
{
    var refreshToken = request.RefreshToken?.Trim();
    if (!string.IsNullOrEmpty(refreshToken))
        await cache.RemoveAsync($"refresh:{refreshToken}", ct);

    return Results.NoContent();
});

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
}).RequireAuthorization();

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
}).RequireAuthorization();

app.MapDelete("/categories/{id:guid}", async (Guid id, StoreDbContext db, CancellationToken ct) =>
{
    var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
    if (category is null) return Results.NotFound();
    db.Categories.Remove(category);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
}).RequireAuthorization();

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
}).RequireAuthorization();

app.MapPut("/customers/{id:guid}", async (Guid id, UpdateCustomerRequest request, StoreDbContext db, CancellationToken ct) =>
{
    var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
    if (customer is null) return Results.NotFound();

    customer.Update(request.Email, request.FirstName, request.LastName, request.Phone);
    await db.SaveChangesAsync(ct);
    return Results.Ok(ToCustomerResponse(customer));
}).RequireAuthorization();

app.MapDelete("/customers/{id:guid}", async (Guid id, StoreDbContext db, CancellationToken ct) =>
{
    var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
    if (customer is null) return Results.NotFound();
    db.Customers.Remove(customer);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
}).RequireAuthorization();

// ---------- Products ----------
app.MapGet("/products", async (
    StoreDbContext db,
    string? search,
    Guid? categoryId,
    decimal? minPrice,
    decimal? maxPrice,
    bool? inStock,
    string? sort,
    int page = 1,
    int pageSize = 20,
    CancellationToken ct = default) =>
{
    var result = await QueryProductsAsync(db, search, categoryId, minPrice, maxPrice, inStock, sort, page, pageSize, ct);
    return Results.Ok(result);
});

// /products/search este un alias peste /products: acceptă `q` (compatibilitate) în plus față de `search`.
app.MapGet("/products/search", async (
    StoreDbContext db,
    string? q,
    string? search,
    Guid? categoryId,
    decimal? minPrice,
    decimal? maxPrice,
    bool? inStock,
    string? sort,
    int page = 1,
    int pageSize = 20,
    CancellationToken ct = default) =>
{
    var term = string.IsNullOrWhiteSpace(search) ? q : search;
    var result = await QueryProductsAsync(db, term, categoryId, minPrice, maxPrice, inStock, sort, page, pageSize, ct);
    return Results.Ok(result);
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
}).RequireAuthorization();

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
}).RequireAuthorization();

app.MapDelete("/products/{id:guid}", async (Guid id, StoreDbContext db, CancellationToken ct) =>
{
    var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
    if (product is null) return Results.NotFound();
    db.Products.Remove(product);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
}).RequireAuthorization();

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
}).RequireAuthorization();

app.MapDelete("/reviews/{id:guid}", async (Guid id, StoreDbContext db, CancellationToken ct) =>
{
    var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == id, ct);
    if (review is null) return Results.NotFound();
    db.Reviews.Remove(review);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
}).RequireAuthorization();

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
}).RequireAuthorization();

// ---------- Warehouses ----------
app.MapGet("/warehouses", async (StoreDbContext db, CancellationToken ct) =>
{
    var warehouses = await db.Warehouses.AsNoTracking().OrderBy(w => w.Name).ToListAsync(ct);
    return Results.Ok(warehouses.Select(ToWarehouseResponse));
});

app.MapPost("/warehouses", async (CreateWarehouseRequest request, StoreDbContext db, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest("Name is required.");
    if (string.IsNullOrWhiteSpace(request.Code))
        return Results.BadRequest("Code is required.");

    var code = request.Code.Trim().ToUpperInvariant();
    if (await db.Warehouses.AnyAsync(w => w.Code == code, ct))
        return Results.Conflict($"Warehouse code '{code}' already exists.");

    var warehouse = new Warehouse(request.Name, code);
    db.Warehouses.Add(warehouse);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/warehouses/{warehouse.Id}", ToWarehouseResponse(warehouse));
}).RequireAuthorization();

app.MapGet("/warehouses/{id:guid}/inventory", async (Guid id, StoreDbContext db, CancellationToken ct) =>
{
    if (!await db.Warehouses.AnyAsync(w => w.Id == id, ct))
        return Results.NotFound();

    var rows = await db.WarehouseInventories.AsNoTracking()
        .Where(wi => wi.WarehouseId == id)
        .ToListAsync(ct);

    var productIds = rows.Select(r => r.ProductId).Distinct().ToList();
    var products = productIds.Count == 0
        ? new Dictionary<Guid, Product>()
        : await db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

    return Results.Ok(rows.Select(r =>
    {
        products.TryGetValue(r.ProductId, out var p);
        return new WarehouseInventoryResponse(r.WarehouseId, r.ProductId, p?.Name ?? "Unknown", p?.Sku ?? string.Empty, r.QuantityOnHand, r.Reserved);
    }));
});

app.MapPut("/warehouses/{warehouseId:guid}/inventory/{productId:guid}", async (Guid warehouseId, Guid productId, SetWarehouseInventoryRequest request, StoreDbContext db, CancellationToken ct) =>
{
    if (request.QuantityOnHand < 0 || request.Reserved < 0)
        return Results.BadRequest("QuantityOnHand and Reserved must be non-negative.");

    var warehouseExists = await db.Warehouses.AnyAsync(w => w.Id == warehouseId, ct);
    if (!warehouseExists) return Results.NotFound();

    var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId, ct);
    if (product is null) return Results.NotFound();

    var wi = await db.WarehouseInventories.FirstOrDefaultAsync(w => w.WarehouseId == warehouseId && w.ProductId == productId, ct);
    if (wi is null)
    {
        wi = new WarehouseInventory(warehouseId, productId, request.QuantityOnHand);
        db.WarehouseInventories.Add(wi);
    }

    wi.SetLevels(request.QuantityOnHand, request.Reserved);
    await db.SaveChangesAsync(ct);

    await SyncAggregateInventoryAsync(db, productId, ct);
    await db.SaveChangesAsync(ct);

    return Results.Ok(new WarehouseInventoryResponse(wi.WarehouseId, wi.ProductId, product.Name, product.Sku, wi.QuantityOnHand, wi.Reserved));
}).RequireAuthorization();

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

    var paymentMethod = PaymentMethod.CashOnDelivery;
    if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
    {
        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, ignoreCase: true, out paymentMethod))
            return Results.BadRequest($"Invalid payment method '{request.PaymentMethod}'.");
    }

    var order = new Order(request.CustomerId, request.Currency ?? "USD", request.Notes, paymentMethod);

    var allocatedProductIds = new HashSet<Guid>();
    if (request.Lines is not null)
    {
        foreach (var line in request.Lines)
        {
            var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == line.ProductId, ct);
            if (product is null) return Results.BadRequest($"Product {line.ProductId} not found.");
            order.AddLine(product.Id, product.Name, product.Price.Amount, line.Quantity);

            // Alocare first-fit: primul depozit cu stoc liber suficient pentru linie.
            var slot = await db.WarehouseInventories
                .Where(wi => wi.ProductId == line.ProductId && wi.QuantityOnHand >= line.Quantity)
                .OrderBy(wi => wi.WarehouseId)
                .FirstOrDefaultAsync(ct);

            if (slot is not null)
            {
                slot.Allocate(line.Quantity);
                allocatedProductIds.Add(line.ProductId);
            }
        }
    }

    db.Orders.Add(order);
    AddEvent(db, "OrderCreated", new { orderId = order.Id, orderNumber = order.OrderNumber, customerId = order.CustomerId });
    await db.SaveChangesAsync(ct);

    foreach (var productId in allocatedProductIds)
        await SyncAggregateInventoryAsync(db, productId, ct);

    if (allocatedProductIds.Count > 0)
        await db.SaveChangesAsync(ct);

    return Results.Created($"/orders/{order.Id}", ToOrderResponse(order));
}).RequireAuthorization();

app.MapPut("/orders/{id:guid}", async (Guid id, UpdateOrderRequest request, StoreDbContext db, CancellationToken ct) =>
{
    var order = await db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, ct);
    if (order is null) return Results.NotFound();

    if (!Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var status))
        return Results.BadRequest($"Invalid status '{request.Status}'.");

    order.SetStatus(status);
    await db.SaveChangesAsync(ct);
    return Results.Ok(ToOrderResponse(order));
}).RequireAuthorization();

app.MapDelete("/orders/{id:guid}", async (Guid id, StoreDbContext db, CancellationToken ct) =>
{
    var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);
    if (order is null) return Results.NotFound();
    db.Orders.Remove(order);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
}).RequireAuthorization();

// ---------- Cart (Redis/cache, per user autentificat) ----------
app.MapGet("/cart", async (ClaimsPrincipal user, IDistributedCache cache, StoreDbContext db, CancellationToken ct) =>
{
    var items = await LoadCartAsync(cache, GetUserId(user), ct);
    return Results.Ok(await BuildCartResponseAsync(db, items, ct));
}).RequireAuthorization();

app.MapPost("/cart/items", async (AddCartItemRequest request, ClaimsPrincipal user, IDistributedCache cache, StoreDbContext db, CancellationToken ct) =>
{
    if (request.Quantity <= 0)
        return Results.BadRequest("Quantity must be greater than zero.");

    if (!await db.Products.AnyAsync(p => p.Id == request.ProductId, ct))
        return Results.NotFound();

    var items = await LoadCartAsync(cache, GetUserId(user), ct);
    var existing = items.FirstOrDefault(i => i.ProductId == request.ProductId);
    if (existing is null)
        items.Add(new CartItem(request.ProductId, request.Quantity));
    else
        existing.Quantity += request.Quantity;

    await SaveCartAsync(cache, GetUserId(user), items, ct);
    return Results.Ok(await BuildCartResponseAsync(db, items, ct));
}).RequireAuthorization();

app.MapPut("/cart/items/{productId:guid}", async (Guid productId, UpdateCartItemRequest request, ClaimsPrincipal user, IDistributedCache cache, StoreDbContext db, CancellationToken ct) =>
{
    if (request.Quantity <= 0)
        return Results.BadRequest("Quantity must be greater than zero.");

    var items = await LoadCartAsync(cache, GetUserId(user), ct);
    var existing = items.FirstOrDefault(i => i.ProductId == productId);
    if (existing is null)
        return Results.NotFound();

    existing.Quantity = request.Quantity;
    await SaveCartAsync(cache, GetUserId(user), items, ct);
    return Results.Ok(await BuildCartResponseAsync(db, items, ct));
}).RequireAuthorization();

app.MapDelete("/cart/items/{productId:guid}", async (Guid productId, ClaimsPrincipal user, IDistributedCache cache, CancellationToken ct) =>
{
    var items = await LoadCartAsync(cache, GetUserId(user), ct);
    items.RemoveAll(i => i.ProductId == productId);
    await SaveCartAsync(cache, GetUserId(user), items, ct);
    return Results.NoContent();
}).RequireAuthorization();

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
        o.PaymentMethod.ToString(),
        o.PaymentStatus.ToString(),
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

WarehouseResponse ToWarehouseResponse(Warehouse w) =>
    new(w.Id, w.Name, w.Code, w.IsActive);

async Task<ProductListResponse> QueryProductsAsync(
    StoreDbContext db,
    string? search,
    Guid? categoryId,
    decimal? minPrice,
    decimal? maxPrice,
    bool? inStock,
    string? sort,
    int page,
    int pageSize,
    CancellationToken ct)
{
    page = Math.Max(1, page);
    pageSize = Math.Clamp(pageSize, 1, 100);

    IQueryable<Product> query = db.Products.AsNoTracking().Where(p => p.IsActive);

    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim();
        query = query.Where(p => p.Name.Contains(term)
            || p.Sku.Contains(term)
            || (p.Description != null && p.Description.Contains(term)));
    }

    if (categoryId.HasValue)
        query = query.Where(p => p.CategoryId == categoryId.Value);

    if (minPrice.HasValue)
        query = query.Where(p => p.Price.Amount >= minPrice.Value);

    if (maxPrice.HasValue)
        query = query.Where(p => p.Price.Amount <= maxPrice.Value);

    if (inStock.HasValue)
    {
        var stockedProductIds = db.Inventories
            .Where(i => i.QuantityOnHand > 0)
            .Select(i => i.ProductId);

        query = inStock.Value
            ? query.Where(p => stockedProductIds.Contains(p.Id))
            : query.Where(p => !stockedProductIds.Contains(p.Id));
    }

    query = (sort ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "price_asc" => query.OrderBy(p => p.Price.Amount),
        "price_desc" => query.OrderByDescending(p => p.Price.Amount),
        "newest" => query.OrderByDescending(p => p.CreatedAt),
        _ => query.OrderBy(p => p.Name),
    };

    var total = await query.CountAsync(ct);
    var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

    return new ProductListResponse(items.Select(ToProductResponse).ToList(), total, page, pageSize);
}

async Task SyncAggregateInventoryAsync(StoreDbContext db, Guid productId, CancellationToken ct)
{
    var rows = await db.WarehouseInventories.AsNoTracking()
        .Where(wi => wi.ProductId == productId)
        .ToListAsync(ct);

    var onHand = rows.Sum(wi => wi.QuantityOnHand);
    var reserved = rows.Sum(wi => wi.Reserved);

    var inventory = await db.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId, ct);
    if (inventory is null)
    {
        inventory = new Inventory(productId, onHand);
        db.Inventories.Add(inventory);
    }

    inventory.SetLevels(onHand, reserved, inventory.ReorderThreshold);
}

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

static Guid GetUserId(ClaimsPrincipal user)
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
    return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
}

static async Task<List<CartItem>> LoadCartAsync(IDistributedCache cache, Guid userId, CancellationToken ct)
{
    var json = await cache.GetStringAsync($"cart:{userId}", ct);
    if (string.IsNullOrEmpty(json))
        return new List<CartItem>();

    return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
}

static async Task SaveCartAsync(IDistributedCache cache, Guid userId, List<CartItem> items, CancellationToken ct)
{
    await cache.SetStringAsync($"cart:{userId}", JsonSerializer.Serialize(items),
        new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(7) }, ct);
}

static async Task<List<CartItemResponse>> BuildCartResponseAsync(StoreDbContext db, List<CartItem> items, CancellationToken ct)
{
    var productIds = items.Select(i => i.ProductId).Distinct().ToList();
    if (productIds.Count == 0)
        return new List<CartItemResponse>();

    var products = await db.Products.AsNoTracking()
        .Where(p => productIds.Contains(p.Id))
        .ToDictionaryAsync(p => p.Id, ct);

    return items
        .Where(i => products.ContainsKey(i.ProductId))
        .Select(i =>
        {
            var p = products[i.ProductId];
            return new CartItemResponse(i.ProductId, p.Name, p.Price.Amount, p.Price.Currency, i.Quantity);
        })
        .ToList();
}

app.Run();

sealed class CartItem
{
    public CartItem(Guid productId, int quantity)
    {
        ProductId = productId;
        Quantity = quantity;
    }

    public Guid ProductId { get; set; }

    public int Quantity { get; set; }
}

sealed record RecommenderItemDto(Guid ProductId, string Name, double Score);

sealed record RecommenderResponseDto(IReadOnlyList<RecommenderItemDto>? Items);

public partial class Program { }
