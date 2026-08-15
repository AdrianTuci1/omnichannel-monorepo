using System.Text.Json;
using System.Text.Json.Serialization;

namespace AkeneoBridge.Models;

/// <summary>Produs Akeneo PIM, așa cum este expus de GET /api/rest/v1/products.</summary>
public sealed class AkeneoProduct
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("family")]
    public string? Family { get; set; }

    [JsonPropertyName("categories")]
    public string[] Categories { get; set; } = Array.Empty<string>();

    [JsonPropertyName("values")]
    public Dictionary<string, AkeneoValue[]> Values { get; set; } = new();

    [JsonPropertyName("created")]
    public DateTime? Created { get; set; }

    [JsonPropertyName("updated")]
    public DateTime? Updated { get; set; }
}

/// <summary>Valoare de atribut Akeneo: pereche locale/scope cu datele asociate.</summary>
public sealed class AkeneoValue
{
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }
}

/// <summary>Atribut Akeneo PIM, așa cum este expus de GET /api/rest/v1/attributes.</summary>
public sealed class AkeneoAttribute
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("group")]
    public string? Group { get; set; }

    [JsonPropertyName("labels")]
    public Dictionary<string, string>? Labels { get; set; }

    [JsonPropertyName("localizable")]
    public bool Localizable { get; set; }

    [JsonPropertyName("scopable")]
    public bool Scopable { get; set; }

    [JsonPropertyName("unique")]
    public bool Unique { get; set; }

    [JsonPropertyName("useable_as_grid_filter")]
    public bool UseableAsGridFilter { get; set; }

    [JsonPropertyName("sort_order")]
    public int SortOrder { get; set; }
}

/// <summary>Categorie Akeneo PIM, folosită pentru rezolvarea numelui categoriei unui produs.</summary>
public sealed class AkeneoCategory
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("parent")]
    public string? Parent { get; set; }

    [JsonPropertyName("labels")]
    public Dictionary<string, string>? Labels { get; set; }
}

/// <summary>Preț Akeneo (colecție de tip price_collection).</summary>
public sealed record AkeneoPrice(decimal Amount, string Currency);

/// <summary>Răspuns OAuth2 pentru obținerea tokenului de acces.</summary>
public sealed class AkeneoTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "bearer";

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
}

/// <summary>Pagină de colecție Akeneo (HATEOAS cu _embedded/_links).</summary>
public sealed class AkeneoPage<T>
{
    [JsonPropertyName("_links")]
    public AkeneoLinks? Links { get; set; }

    [JsonPropertyName("current_page")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("_embedded")]
    public AkeneoEmbedded<T>? Embedded { get; set; }
}

/// <summary>Payload de scriere (upsert) pentru un produs Akeneo PIM.</summary>
public sealed class AkeneoProductWrite
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("family")]
    public string? Family { get; set; }

    [JsonPropertyName("categories")]
    public string[] Categories { get; set; } = Array.Empty<string>();

    [JsonPropertyName("values")]
    public Dictionary<string, AkeneoValueWrite[]> Values { get; set; } = new();
}

/// <summary>Valoare de atribut Akeneo pentru scriere (locale/scope opționale + data).</summary>
public sealed class AkeneoValueWrite
{
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

/// <summary>Intrare de preț (price_collection) în formatul așteptat de Akeneo.</summary>
public sealed record AkeneoPriceWrite(
    [property: JsonPropertyName("amount")] string Amount,
    [property: JsonPropertyName("currency")] string Currency);

/// <summary>Payload de scriere (upsert) pentru un atribut Akeneo PIM.</summary>
public sealed class AkeneoAttributeWrite
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("localizable")]
    public bool Localizable { get; set; }

    [JsonPropertyName("scopable")]
    public bool Scopable { get; set; }

    [JsonPropertyName("group")]
    public string Group { get; set; } = "other";

    [JsonPropertyName("unique")]
    public bool Unique { get; set; }

    [JsonPropertyName("useable_as_grid_filter")]
    public bool UseableAsGridFilter { get; set; }

    [JsonPropertyName("labels")]
    public Dictionary<string, string>? Labels { get; set; }
}

public sealed class AkeneoEmbedded<T>
{
    [JsonPropertyName("items")]
    public T[] Items { get; set; } = Array.Empty<T>();
}

public sealed class AkeneoLinks
{
    [JsonPropertyName("next")]
    public AkeneoLink? Next { get; set; }
}

public sealed class AkeneoLink
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;
}
