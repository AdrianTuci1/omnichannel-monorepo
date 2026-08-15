using System.Globalization;
using System.Text.Json;
using AkeneoBridge.Configuration;
using AkeneoBridge.Models;

namespace AkeneoBridge.Services;

/// <summary>
/// Mapează un produs Akeneo PIM pe forma internă folosită de store-api.
/// Codurile de atribut (nume, descriere, preț) sunt configurabile prin <see cref="AkeneoOptions"/>.
/// </summary>
public sealed class AkeneoProductMapper
{
    private readonly AkeneoOptions _options;

    public AkeneoProductMapper(AkeneoOptions options) => _options = options;

    public MappedProduct Map(AkeneoProduct source)
    {
        var values = source.Values ?? new Dictionary<string, AkeneoValue[]>();
        var categories = source.Categories ?? Array.Empty<string>();

        var sku = source.Identifier.Trim();
        var name = ExtractText(values, _options.NameAttributeCode) ?? sku;
        var description = ExtractText(values, _options.DescriptionAttributeCode);
        var price = ExtractPrice(values, _options.PriceAttributeCode);
        var categoryCode = categories.FirstOrDefault();

        return new MappedProduct(
            sku,
            name,
            description,
            price?.Amount ?? 0m,
            price?.Currency ?? _options.DefaultCurrency,
            categoryCode);
    }

    private static string? ExtractText(IReadOnlyDictionary<string, AkeneoValue[]> values, string code)
    {
        if (!values.TryGetValue(code, out var list) || list is null)
            return null;

        return ExtractScalar(list, static v => v.Data.ValueKind == JsonValueKind.String
            ? v.Data.GetString()
            : null);
    }

    private static AkeneoPrice? ExtractPrice(IReadOnlyDictionary<string, AkeneoValue[]> values, string code)
    {
        if (!values.TryGetValue(code, out var list) || list is null)
            return null;

        foreach (var value in list)
        {
            if (value.Data.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var element in value.Data.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                    continue;

                if (element.TryGetProperty("amount", out var amountElement) &&
                    element.TryGetProperty("currency", out var currencyElement))
                {
                    var amountText = amountElement.GetString();
                    var currency = currencyElement.GetString();

                    if (decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
                        && !string.IsNullOrWhiteSpace(currency))
                    {
                        return new AkeneoPrice(amount, currency);
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Extraordinea valorilor: mai întâi valoarea fără scope și fără locale (canalul default),
    /// apoi prima valoare disponibilă.
    /// </summary>
    private static string? ExtractScalar(AkeneoValue[] list, Func<AkeneoValue, string?> extractor)
    {
        foreach (var value in list)
        {
            if (value.Scope is null && value.Locale is null)
            {
                var text = extractor(value);
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }
        }

        foreach (var value in list)
        {
            var text = extractor(value);
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return null;
    }
}

/// <summary>Produs mapat, pregătit pentru persistență în store-api.</summary>
public sealed record MappedProduct(
    string Sku,
    string Name,
    string? Description,
    decimal PriceAmount,
    string PriceCurrency,
    string? CategoryCode);
