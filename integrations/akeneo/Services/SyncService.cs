using System.Globalization;
using AkeneoBridge.Clients;
using AkeneoBridge.Configuration;
using AkeneoBridge.Models;

namespace AkeneoBridge.Services;

/// <summary>
/// Orchestrează un ciclu complet de sincronizare Akeneo PIM -> store-api:
/// preia atribute, categorii și produse, apoi reconciliază produsele după SKU.
/// Implementează și direcția inversă (store-api -> Akeneo) prin <see cref="RunReverseAsync"/>.
/// </summary>
public sealed class SyncService
{
    private readonly AkeneoClient _akeneo;
    private readonly StoreApiClient _storeApi;
    private readonly AkeneoProductMapper _mapper;
    private readonly SyncOptions _syncOptions;
    private readonly AkeneoOptions _akeneoOptions;

    public SyncService(
        AkeneoClient akeneo,
        StoreApiClient storeApi,
        AkeneoProductMapper mapper,
        SyncOptions syncOptions,
        AkeneoOptions akeneoOptions)
    {
        _akeneo = akeneo;
        _storeApi = storeApi;
        _mapper = mapper;
        _syncOptions = syncOptions;
        _akeneoOptions = akeneoOptions;
    }

    public async Task<SyncResult> RunAsync(CancellationToken ct)
    {
        var attributes = _syncOptions.AttributesEnabled
            ? await CollectAsync(_akeneo.GetAllAttributesAsync(), ct)
            : new List<AkeneoAttribute>();

        var akeneoCategories = await CollectAsync(_akeneo.GetAllCategoriesAsync(), ct);
        var categoryLabels = BuildCategoryLabels(akeneoCategories);

        var products = _syncOptions.ProductsEnabled
            ? await CollectAsync(_akeneo.GetAllProductsAsync(), ct)
            : new List<AkeneoProduct>();

        var storeCategories = await _storeApi.GetCategoriesAsync(ct);
        var slugToId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in storeCategories)
            slugToId[category.Slug] = category.Id;

        var defaultCategoryId = storeCategories.Count > 0 ? storeCategories[0].Id : (Guid?)null;

        var storeProducts = await _storeApi.GetProductsAsync(ct);
        var skuToId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var product in storeProducts)
            skuToId[product.Sku.Trim().ToUpperInvariant()] = product.Id;

        var result = new SyncResult
        {
            AttributesFetched = attributes.Count,
            CategoriesFetched = akeneoCategories.Count,
            ProductsFetched = products.Count,
        };

        async Task<Guid> ResolveCategoryAsync(string? code, CancellationToken token)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                var slug = code.ToLowerInvariant();
                if (slugToId.TryGetValue(slug, out var existing))
                    return existing;

                var name = categoryLabels.TryGetValue(code, out var label) ? label : code;
                var id = await _storeApi.CreateCategoryAsync(new CreateCategoryRequest(name, slug), token);
                slugToId[slug] = id;
                result.CategoriesCreated++;
                return id;
            }

            if (defaultCategoryId is Guid fallback)
                return fallback;

            var defaultId = await _storeApi.CreateCategoryAsync(
                new CreateCategoryRequest("General", "general"), token);
            slugToId["general"] = defaultId;
            defaultCategoryId = defaultId;
            result.CategoriesCreated++;
            return defaultId;
        }

        foreach (var product in products)
        {
            ct.ThrowIfCancellationRequested();

            if (!product.Enabled)
            {
                result.ProductsSkipped++;
                continue;
            }

            var mapped = _mapper.Map(product);
            if (string.IsNullOrWhiteSpace(mapped.Sku))
            {
                result.ProductsSkipped++;
                continue;
            }

            var categoryId = await ResolveCategoryAsync(mapped.CategoryCode, ct);
            var skuKey = mapped.Sku.ToUpperInvariant();

            if (skuToId.TryGetValue(skuKey, out var productId))
            {
                await _storeApi.UpdateProductAsync(
                    productId,
                    new UpdateProductRequest(
                        mapped.Name,
                        mapped.PriceAmount,
                        mapped.PriceCurrency,
                        categoryId,
                        mapped.Description),
                    ct);
                result.ProductsUpdated++;
            }
            else
            {
                var createdId = await _storeApi.CreateProductAsync(
                    new CreateProductRequest(
                        mapped.Sku,
                        mapped.Name,
                        mapped.PriceAmount,
                        mapped.PriceCurrency,
                        mapped.Description,
                        categoryId),
                    ct);
                skuToId[skuKey] = createdId;
                result.ProductsCreated++;
            }
        }

        return result;
    }

    /// <summary>
    /// Direcția inversă (store-api -> Akeneo): asigură existența atributelor configurate
    /// (nume, descriere, preț) în Akeneo, apoi exportă produsele active din store-api.
    /// </summary>
    public async Task<ReverseSyncResult> RunReverseAsync(CancellationToken ct)
    {
        await EnsureAttributesAsync(ct);

        var storeProducts = await _storeApi.GetProductsFullAsync(ct);
        var storeCategories = await _storeApi.GetCategoriesAsync(ct);
        var slugByCategoryId = storeCategories.ToDictionary(c => c.Id, c => c.Slug);

        var result = new ReverseSyncResult { ProductsFetched = storeProducts.Count };

        foreach (var product in storeProducts)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(product.Sku))
            {
                result.ProductsSkipped++;
                continue;
            }

            var categories = product.CategoryId != Guid.Empty
                && slugByCategoryId.TryGetValue(product.CategoryId, out var slug)
                    ? new[] { slug }
                    : Array.Empty<string>();

            await _akeneo.UpsertProductAsync(MapStoreProductToAkeneo(product, categories), ct);
            result.ProductsUpserted++;
        }

        return result;
    }

    private async Task EnsureAttributesAsync(CancellationToken ct)
    {
        var attributes = new[]
        {
            new AkeneoAttributeWrite
            {
                Code = _akeneoOptions.NameAttributeCode,
                Type = "pim_catalog_text",
                Group = "other",
                Labels = new Dictionary<string, string> { ["en_US"] = "Name" },
            },
            new AkeneoAttributeWrite
            {
                Code = _akeneoOptions.DescriptionAttributeCode,
                Type = "pim_catalog_textarea",
                Group = "other",
                Labels = new Dictionary<string, string> { ["en_US"] = "Description" },
            },
            new AkeneoAttributeWrite
            {
                Code = _akeneoOptions.PriceAttributeCode,
                Type = "pim_catalog_price_collection",
                Group = "other",
                Labels = new Dictionary<string, string> { ["en_US"] = "Price" },
            },
        };

        foreach (var attribute in attributes)
            await _akeneo.UpsertAttributeAsync(attribute, ct);
    }

    private AkeneoProductWrite MapStoreProductToAkeneo(StoreProductFullResponse product, string[] categories)
    {
        var values = new Dictionary<string, AkeneoValueWrite[]>
        {
            [_akeneoOptions.NameAttributeCode] =
                new[] { new AkeneoValueWrite { Data = product.Name } },
            [_akeneoOptions.PriceAttributeCode] =
                new[]
                {
                    new AkeneoValueWrite
                    {
                        Data = new[]
                        {
                            new AkeneoPriceWrite(
                                product.PriceAmount.ToString("0.00", CultureInfo.InvariantCulture),
                                string.IsNullOrWhiteSpace(product.PriceCurrency)
                                    ? _akeneoOptions.DefaultCurrency
                                    : product.PriceCurrency),
                        },
                    },
                },
        };

        if (!string.IsNullOrWhiteSpace(product.Description))
        {
            values[_akeneoOptions.DescriptionAttributeCode] =
                new[] { new AkeneoValueWrite { Data = product.Description } };
        }

        return new AkeneoProductWrite
        {
            Identifier = product.Sku,
            Enabled = true,
            Categories = categories,
            Values = values,
        };
    }

    private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> source, CancellationToken ct)
    {
        var list = new List<T>();
        await foreach (var item in source.WithCancellation(ct))
            list.Add(item);

        return list;
    }

    private static Dictionary<string, string> BuildCategoryLabels(IReadOnlyCollection<AkeneoCategory> categories)
    {
        var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in categories)
        {
            var label = category.Labels?.Values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(label))
                labels[category.Code] = label;
        }

        return labels;
    }
}

/// <summary>Rezumatul unui ciclu de sincronizare.</summary>
public sealed class SyncResult
{
    public int AttributesFetched { get; set; }

    public int CategoriesFetched { get; set; }

    public int CategoriesCreated { get; set; }

    public int ProductsFetched { get; set; }

    public int ProductsCreated { get; set; }

    public int ProductsUpdated { get; set; }

    public int ProductsSkipped { get; set; }
}

/// <summary>Rezumatul unui ciclu de sincronizare inversă (store-api -> Akeneo).</summary>
public sealed class ReverseSyncResult
{
    public int ProductsFetched { get; set; }

    public int ProductsUpserted { get; set; }

    public int ProductsSkipped { get; set; }
}
