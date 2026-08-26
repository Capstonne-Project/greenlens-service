using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Greenlens.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenlens.Infrastructure.Ai;

/// <summary>
/// Text-only Gemini call to draft a short, professional Vietnamese report description
/// from the already-classified category/severity/subtypes. Best-effort: any failure
/// (missing key, timeout, non-2xx, malformed response) returns null and never throws,
/// so a slow/unavailable LLM never blocks the analyze endpoint.
/// </summary>
internal sealed class GeminiReportDescriptionGenerator(
    IHttpClientFactory httpClientFactory,
    IOptions<GeminiOptions> options,
    ILogger<GeminiReportDescriptionGenerator> logger)
    : IReportDescriptionGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<string?> GenerateAsync(ReportDescriptionContext context, CancellationToken ct = default)
    {
        var opts = options.Value;

        if (string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            return null;
        }

        try
        {
            using var client = httpClientFactory.CreateClient("Gemini");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(opts.TimeoutSeconds));

            var prompt = BuildPrompt(context);
            var requestBody = new GeminiRequest(
                Contents: [new GeminiContent([new GeminiPart(prompt)])],
                GenerationConfig: new GeminiGenerationConfig(MaxOutputTokens: 200, Temperature: 0.6));

            var endpoint = $"{opts.BaseUrl.TrimEnd('/')}/models/{opts.Model}:generateContent?key={opts.ApiKey}";

            using var content = new StringContent(
                JsonSerializer.Serialize(requestBody, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(endpoint, content, cts.Token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "[Gemini] description FAIL HTTP {StatusCode}",
                    (int)response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            var raw = JsonSerializer.Deserialize<GeminiResponse>(json, JsonOptions);

            var text = raw?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("[Gemini] description TIMEOUT after {TimeoutSec}s", opts.TimeoutSeconds);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Gemini] description EXCEPTION");
            return null;
        }
    }

    private static string BuildPrompt(ReportDescriptionContext context)
    {
        var subtypes = context.Subtypes.Count > 0
            ? string.Join(", ", context.Subtypes.Select(s => s.Label))
            : "rác thải chưa rõ loại";

        var coveragePct = Math.Round(context.PollutionCoverageRatio * 100, 0);
        var coverageDesc = coveragePct switch
        {
            <= 0 => null,
            < 15 => "chỉ một góc nhỏ",
            < 40 => "một khoảng khá lớn",
            < 70 => "lan ra cả một khu vực rộng",
            _ => "tràn ngập gần như toàn bộ khu vực",
        };

        return $"""
            Bạn đang đóng vai một người dân bình thường vừa chụp ảnh và báo cáo một điểm ô nhiễm
            qua ứng dụng. Dựa vào các dữ kiện AI đã nhận diện được từ ảnh dưới đây, hãy viết lại
            THAY cho người đó một đoạn mô tả ngắn (1-2 câu, tối đa 50 từ) bằng tiếng Việt, đúng
            như cách một người dân bình thường kể lại những gì họ thấy — văn nói tự nhiên, đời
            thường, KHÔNG phải văn phong hành chính/biên bản/báo cáo, không dùng từ ngữ kiểu
            "ghi nhận", "ghi nhận tình trạng", "mức độ nghiêm trọng", "khu vực quan sát".

            Những gì AI nhận diện được từ ảnh (chỉ dùng để tham khảo, không liệt kê lại y nguyên):
            - Loại ô nhiễm: {context.CategoryNameVi}
            - Mức độ nghiêm trọng theo AI: {context.Severity}
            - Rác/tác nhân nhìn thấy: {subtypes}
            {(coverageDesc is not null ? $"- Mức độ lan rộng trong ảnh: {coverageDesc}" : "")}

            Yêu cầu:
            - Viết như đang kể chuyện/mô tả bằng miệng, ví dụ kiểu "Thấy có ... vứt bừa bãi ở ...",
              "Khu vực này có nhiều ... rất mất vệ sinh", v.v.
            - Nhắc tới loại rác/tác nhân cụ thể đã thấy, không nói chung chung.
            - Nếu mức độ lan rộng lớn, thể hiện điều đó qua giọng văn (ví dụ mức độ bức xúc, quan ngại).
            - Không dùng markdown, không thêm tiêu đề hay ghi chú, chỉ trả về đúng đoạn mô tả.
            """;
    }

    // ── Gemini REST request/response models ──────────────────────────────

    private sealed record GeminiRequest(
        [property: JsonPropertyName("contents")] IReadOnlyList<GeminiContent> Contents,
        [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig);

    private sealed record GeminiContent(
        [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

    private sealed record GeminiPart([property: JsonPropertyName("text")] string Text);

    private sealed record GeminiGenerationConfig(
        [property: JsonPropertyName("maxOutputTokens")] int MaxOutputTokens,
        [property: JsonPropertyName("temperature")] double Temperature);

    private sealed class GeminiResponse
    {
        [JsonPropertyName("candidates")] public List<GeminiCandidate>? Candidates { get; init; }
    }

    private sealed class GeminiCandidate
    {
        [JsonPropertyName("content")] public GeminiResponseContent? Content { get; init; }
    }

    private sealed class GeminiResponseContent
    {
        [JsonPropertyName("parts")] public List<GeminiResponsePart>? Parts { get; init; }
    }

    private sealed class GeminiResponsePart
    {
        [JsonPropertyName("text")] public string? Text { get; init; }
    }
}
