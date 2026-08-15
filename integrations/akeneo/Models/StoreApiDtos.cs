namespace AkeneoBridge.Models;

/// <summary>Categorie store-api (subset folosit pentru rezolvarea după slug).</summary>
public sealed record StoreCategoryResponse(Guid Id, string Name, string Slug);

/// <summary>Produs store-api (subset folosit pentru detecția după SKU).</summary>
public sealed record StoreProductResponse(Guid Id, string Sku);

/// <summary>Produs store-api complet (folosit pentru exportul invers către Akeneo).</summary>
public sealed record StoreProductFullResponse(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal PriceAmount,
    string PriceCurrency,
    Guid CategoryId,
    bool IsActive,
    DateTime CreatedAt);

/// <summary>Răspuns generic store-api care expune doar identificatorul entității create.</summary>
public sealed record StoreEntityResponse(Guid Id);

/// <summary>Cerere de creare categorie store-api.</summary>
public sealed record CreateCategoryRequest(string Name, string Slug);

/// <summary>Cerere de creare produs store-api.</summary>
public sealed record CreateProductRequest(
    string Sku,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    string? Description,
    Guid? CategoryId);

/// <summary>Cerere de actualizare produs store-api.</summary>
public sealed record UpdateProductRequest(
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    Guid CategoryId,
    string? Description);
