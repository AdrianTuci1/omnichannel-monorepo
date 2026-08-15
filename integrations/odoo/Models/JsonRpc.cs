using System.Text.Json;
using System.Text.Json.Serialization;

namespace OdooBridge.Models;

/// <summary>Cadru JSON-RPC 2.0 folosit de API-ul extern Odoo.</summary>
public sealed class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    [JsonPropertyName("method")]
    public string Method { get; init; } = "call";

    [JsonPropertyName("params")]
    public JsonRpcParams Params { get; init; } = new();

    [JsonPropertyName("id")]
    public int Id { get; init; }
}

public sealed class JsonRpcParams
{
    [JsonPropertyName("service")]
    public string Service { get; init; } = "object";

    [JsonPropertyName("method")]
    public string Method { get; init; } = string.Empty;

    [JsonPropertyName("args")]
    public JsonElement[] Args { get; init; } = Array.Empty<JsonElement>();
}

public sealed class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("result")]
    public JsonElement? Result { get; set; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }

    public bool IsSuccess => Error is null && Result is not null;
}

public sealed class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }
}
