using System.Text.Json.Serialization;

namespace Greenlens.Application.Common.Models;

public sealed class ApiResponse<T>
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = "SUCCESS";

    [JsonPropertyName("message")]
    public string Message { get; init; } = "OK";

    [JsonPropertyName("status")]
    public int Status { get; init; } = 200;

    [JsonPropertyName("data")]
    public T? Data { get; init; }
}

public sealed class ApiResponse
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = "SUCCESS";

    [JsonPropertyName("message")]
    public string Message { get; init; } = "OK";

    [JsonPropertyName("status")]
    public int Status { get; init; } = 200;

    [JsonPropertyName("data")]
    public object? Data { get; init; }
}
