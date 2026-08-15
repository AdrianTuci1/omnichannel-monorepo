using System.Text.Json;

namespace OdooBridge.Models;

/// <summary>Utilitare pentru citirea referințelor many2one ([id, nume]) din răspunsurile Odoo.</summary>
public static class OdooRefs
{
    /// <summary>Extrage ID-ul dintr-o referință many2one ([id, nume]); 0 dacă referința este goală (false).</summary>
    public static int GetId(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array
            && element.GetArrayLength() >= 1
            && element[0].ValueKind == JsonValueKind.Number)
        {
            return element[0].GetInt32();
        }

        return 0;
    }

    /// <summary>Extrage numele dintr-o referință many2one ([id, nume]); string gol dacă nu există.</summary>
    public static string GetName(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array
            && element.GetArrayLength() >= 2
            && element[1].ValueKind == JsonValueKind.String)
        {
            return element[1].GetString() ?? string.Empty;
        }

        return string.Empty;
    }
}
