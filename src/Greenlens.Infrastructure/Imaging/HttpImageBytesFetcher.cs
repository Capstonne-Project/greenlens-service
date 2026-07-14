using Greenlens.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Imaging;

public sealed class HttpImageBytesFetcher(
    IHttpClientFactory httpClientFactory,
    ILogger<HttpImageBytesFetcher> logger) : IImageBytesFetcher
{
    public async Task<byte[]?> TryFetchAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var client = httpClientFactory.CreateClient("ImageFetch");
            await using var stream = await client.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not download image bytes for EXIF analysis");
            return null;
        }
    }
}
