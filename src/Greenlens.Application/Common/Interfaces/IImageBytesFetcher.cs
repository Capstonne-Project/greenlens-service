namespace Greenlens.Application.Common.Interfaces;

/// <summary>Downloads image bytes from a public URL for EXIF analysis (BR-REP-011 manual submit flow).</summary>
public interface IImageBytesFetcher
{
    Task<byte[]?> TryFetchAsync(string url, CancellationToken cancellationToken);
}
