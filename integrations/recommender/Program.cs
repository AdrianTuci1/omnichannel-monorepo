using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Recommender.Api;
using Recommender.Api.Clients;
using Recommender.Api.Configuration;
using Recommender.Api.Domain;
using Recommender.Api.Embeddings;
using Recommender.Api.Persistence;
using Recommender.Api.Recommendations;

var builder = WebApplication.CreateBuilder(args);

var options = builder.Configuration.GetSection(RecommenderOptions.SectionName).Get<RecommenderOptions>()
    ?? new RecommenderOptions();
var storeApiOptions = builder.Configuration.GetSection(StoreApiOptions.SectionName).Get<StoreApiOptions>()
    ?? new StoreApiOptions();

builder.Services.AddSingleton(options);
builder.Services.AddSingleton(storeApiOptions);
builder.Services.AddSingleton<IEmbeddingService, HashingEmbeddingService>();
builder.Services.AddSingleton<IEmbeddingStore, InMemoryEmbeddingStore>();

var dbRoot = new InMemoryDatabaseRoot();
builder.Services.AddSingleton(dbRoot);
builder.Services.AddDbContext<RecommenderDbContext>(db =>
    db.UseInMemoryDatabase("Recommender", dbRoot));

builder.Services.AddScoped<IContentBasedRecommender, ContentBasedRecommender>();
builder.Services.AddScoped<ICollaborativeRecommender, CollaborativeRecommender>();
builder.Services.AddScoped<IHybridRecommender, HybridRecommender>();

builder.Services.AddHttpClient<IStoreApiClient, StoreApiClient>(client =>
{
    client.BaseAddress = new Uri(storeApiOptions.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddSingleton<StoreDataSynchronizer>();
builder.Services.AddSingleton<IStoreDataSynchronizer>(sp => sp.GetRequiredService<StoreDataSynchronizer>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<StoreDataSynchronizer>());

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/health/ready", async (RecommenderDbContext db, CancellationToken ct) =>
{
    var ready = await db.Products.AnyAsync(ct);
    return ready
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.MapGet("/recommendations/{productId:guid}", GetRelatedProducts);
app.MapGet("/products/{productId:guid}/related", GetRelatedProducts);

app.MapGet("/recommendations/customer/{customerId:guid}", async (
    Guid customerId,
    IStoreDataSynchronizer sync,
    IHybridRecommender hybrid,
    ICollaborativeRecommender collaborative,
    RecommenderOptions options,
    int? limit,
    string? strategy,
    double? contentWeight,
    CancellationToken ct) =>
{
    await sync.EnsureLoadedAsync(ct);

    var resolvedLimit = ResolveLimit(limit, options);
    var resolvedWeight = Math.Clamp(contentWeight ?? options.ContentWeight, 0.0, 1.0);
    var useHybrid = !string.Equals(strategy?.Trim().ToLowerInvariant(), "collaborative", StringComparison.Ordinal);

    var items = useHybrid
        ? await hybrid.RecommendForCustomerAsync(customerId, resolvedLimit, resolvedWeight, ct)
        : await collaborative.RecommendForCustomerAsync(customerId, resolvedLimit, ct);

    var strategyName = useHybrid ? "hybrid" : "collaborative";

    return Results.Ok(new CustomerRecommendationResponse(
        customerId, strategyName, resolvedWeight, resolvedLimit, items));
});

app.MapGet("/recommendations/search", async (
    string? text,
    IStoreDataSynchronizer sync,
    IContentBasedRecommender content,
    RecommenderOptions options,
    int? limit,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(text))
        return Results.BadRequest(new { error = "Query text is required." });

    await sync.EnsureLoadedAsync(ct);

    var resolvedLimit = ResolveLimit(limit, options);
    var items = await content.SearchByTextAsync(text, resolvedLimit, ct);

    return Results.Ok(new TextRecommendationResponse(
        text.Trim(), RecommendationStrategy.ContentBased.ToString().ToLowerInvariant(), resolvedLimit, items));
});

app.Run();

async Task<IResult> GetRelatedProducts(
    Guid productId,
    IStoreDataSynchronizer sync,
    IHybridRecommender hybrid,
    IContentBasedRecommender content,
    ICollaborativeRecommender collaborative,
    RecommenderOptions options,
    int? limit,
    string? strategy,
    double? contentWeight,
    CancellationToken ct)
{
    await sync.EnsureLoadedAsync(ct);

    var resolvedLimit = ResolveLimit(limit, options);
    var resolvedStrategy = ResolveStrategy(strategy);
    var resolvedWeight = Math.Clamp(contentWeight ?? options.ContentWeight, 0.0, 1.0);

    var items = resolvedStrategy switch
    {
        RecommendationStrategy.ContentBased => await content.RecommendAsync(productId, resolvedLimit, ct),
        RecommendationStrategy.Collaborative => await collaborative.RecommendAsync(productId, resolvedLimit, ct),
        _ => await hybrid.RecommendAsync(productId, resolvedLimit, resolvedWeight, ct),
    };

    return Results.Ok(items.Select(i => new RelatedProductResponse(i.ProductId, i.Name, i.Score)));
}

int ResolveLimit(int? limit, RecommenderOptions options)
    => Math.Clamp(limit ?? options.DefaultLimit, 1, options.MaxLimit);

RecommendationStrategy ResolveStrategy(string? strategy) => strategy?.Trim().ToLowerInvariant() switch
{
    "content" or "contentbased" or "content-based" => RecommendationStrategy.ContentBased,
    "collaborative" or "collab" or "item-item" => RecommendationStrategy.Collaborative,
    _ => RecommendationStrategy.Hybrid,
};

public partial class Program { }
