using System.Text.Json;
using System.Text.Json.Serialization;

namespace OdooBridge.Models;

/// <summary>Produs sincronizat din Odoo (product.template).</summary>
public sealed class OdooProduct
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("default_code")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("list_price")]
    public decimal PriceAmount { get; set; }

    [JsonPropertyName("currency_id")]
    public JsonElement CurrencyRef { get; set; }

    [JsonPropertyName("description_sale")]
    public string? Description { get; set; }

    [JsonPropertyName("active")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("categ_id")]
    public JsonElement CategoryRef { get; set; }

    [JsonPropertyName("write_date")]
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Comandă sincronizată din Odoo (sale.order).</summary>
public sealed class OdooOrder
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("partner_id")]
    public JsonElement PartnerRef { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = "draft";

    [JsonPropertyName("currency_id")]
    public JsonElement CurrencyRef { get; set; }

    [JsonPropertyName("order_line")]
    public int[] OrderLineIds { get; set; } = Array.Empty<int>();

    [JsonPropertyName("date_order")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("amount_total")]
    public decimal AmountTotal { get; set; }
}

/// <summary>Linie de comandă din Odoo (sale.order.line).</summary>
public sealed class OdooOrderLine
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("order_id")]
    public JsonElement OrderRef { get; set; }

    [JsonPropertyName("product_id")]
    public JsonElement ProductRef { get; set; }

    [JsonPropertyName("name")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("product_uom_qty")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("price_unit")]
    public decimal UnitPrice { get; set; }
}

/// <summary>Partener (client) din Odoo (res.partner).</summary>
public sealed class OdooPartner
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
}

/// <summary>Referință Odoo many2one (id + nume).</summary>
public sealed class OdooReference
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

/// <summary>Variantă de produs din Odoo (product.product), folosită pentru maparea SKU pe liniile de comandă.</summary>
public sealed class OdooProductVariant
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("default_code")]
    public string Sku { get; set; } = string.Empty;
}
