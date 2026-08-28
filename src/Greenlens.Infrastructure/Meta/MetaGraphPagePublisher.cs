using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Options;
using Greenlens.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenlens.Infrastructure.Meta;

/// <summary>
/// Posts photo updates to a Facebook Page via Meta Graph API.
/// </summary>
internal sealed class MetaGraphPagePublisher(
    IHttpClientFactory httpClientFactory,
    IOptions<MetaPageOptions> options,
    ILogger<MetaGraphPagePublisher> logger)
    : IFacebookPagePublisher
{
    public async Task<Result<string>> PublishPhotoPostAsync(
        string caption,
        string imageUrl,
        CancellationToken ct = default)
    {
        var opts = options.Value;
        if (!opts.IsConfigured)
        {
            logger.LogWarning("Facebook Page share skipped: Meta PageId or PageAccessToken is not configured");
            return Errors.Meta.NotConfigured;
        }

        if (string.IsNullOrWhiteSpace(imageUrl))
            return Errors.Meta.ShareImageRequired;

        var client = httpClientFactory.CreateClient("MetaGraph");
        var path = $"/{opts.GraphApiVersion.Trim('/')}/{opts.PageId}/photos";

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["caption"] = caption,
            ["url"] = imageUrl,
            ["access_token"] = opts.PageAccessToken
        });

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(path, form, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Facebook Graph API request failed for Page {PageId}", opts.PageId);
            return Errors.Meta.PublishFailed("Không thể kết nối Meta Graph API.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await TryReadGraphErrorMessageAsync(response, ct).ConfigureAwait(false);
                logger.LogWarning(
                    "Facebook Page photo post failed HTTP {StatusCode} for Page {PageId}: {ErrorMessage}",
                    (int)response.StatusCode,
                    opts.PageId,
                    errorMessage);

                return Errors.Meta.PublishFailed(errorMessage);
            }

            var payload = await response.Content
                .ReadFromJsonAsync<GraphPhotoResponse>(cancellationToken: ct)
                .ConfigureAwait(false);

            var postId = payload?.PostId ?? payload?.Id;
            if (postId is null or { Length: 0 })
            {
                logger.LogWarning("Facebook Page photo post returned success but no post id for Page {PageId}", opts.PageId);
                return Errors.Meta.PublishFailed("Meta Graph API không trả về post id.");
            }

            logger.LogInformation("Facebook Page photo post succeeded for Page {PageId}, post {PostId}", opts.PageId, postId);
            return postId;
        }
    }

    private static async Task<string> TryReadGraphErrorMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var errorPayload = await response.Content
                .ReadFromJsonAsync<GraphErrorEnvelope>(cancellationToken: ct)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(errorPayload?.Error?.Message))
                return errorPayload.Error.Message;
        }
        catch
        {
            // Fall back to generic message below.
        }

        return $"Meta Graph API trả về HTTP {(int)response.StatusCode}.";
    }

    private sealed record GraphPhotoResponse(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("post_id")] string? PostId);

    private sealed record GraphErrorEnvelope([property: JsonPropertyName("error")] GraphError? Error);

    private sealed record GraphError(
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("code")] int? Code);
}
