using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.UnitTests;

public sealed class ReportMediaTests
{
    [Fact]
    public void Create_ShouldSetAllFields()
    {
        var reportId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var media = ReportMedia.Create(
            reportId, MediaType.Image, "https://cdn/image.jpg",
            "image/jpeg", 2_048_576, userId,
            thumbnailUrl: "https://cdn/thumb.jpg",
            width: 1920, height: 1080,
            pHash: "abc123", exifData: "{\"device\":\"iPhone\"}");

        Assert.Equal(reportId, media.ReportId);
        Assert.Equal(MediaType.Image, media.Type);
        Assert.Equal("https://cdn/image.jpg", media.Url);
        Assert.Equal("https://cdn/thumb.jpg", media.ThumbnailUrl);
        Assert.Equal("image/jpeg", media.MimeType);
        Assert.Equal(2_048_576, media.SizeBytes);
        Assert.Equal(1920, media.Width);
        Assert.Equal(1080, media.Height);
        Assert.Equal("abc123", media.PHash);
        Assert.Equal("{\"device\":\"iPhone\"}", media.ExifData);
        Assert.Equal(userId, media.UploadedBy);
    }

    [Fact]
    public void ChangeType_ShouldUpdateType()
    {
        var media = ReportMedia.Create(Guid.NewGuid(), MediaType.Image,
            "url", "image/jpeg", 1000, null);

        media.ChangeType(MediaType.Before);

        Assert.Equal(MediaType.Before, media.Type);
    }

    [Fact]
    public void ReassignToReport_MovesMediaToPrimary_AndSetsSourceReportId_BR_REP_032()
    {
        var originalReportId = Guid.NewGuid();
        var primaryReportId = Guid.NewGuid();
        var media = ReportMedia.Create(originalReportId, MediaType.Image,
            "https://cdn/dup.jpg", "image/jpeg", 1000, null);

        media.ReassignToReport(primaryReportId);

        Assert.Equal(primaryReportId, media.ReportId);
        Assert.Equal(originalReportId, media.SourceReportId);
    }

    [Fact]
    public void ReassignToReport_KeepsFirstSourceReportId_WhenReassignedAgain_BR_REP_032()
    {
        var originalReportId = Guid.NewGuid();
        var firstPrimaryId = Guid.NewGuid();
        var secondPrimaryId = Guid.NewGuid();
        var media = ReportMedia.Create(originalReportId, MediaType.Image,
            "https://cdn/dup.jpg", "image/jpeg", 1000, null);

        media.ReassignToReport(firstPrimaryId);
        media.ReassignToReport(secondPrimaryId);

        Assert.Equal(secondPrimaryId, media.ReportId);
        Assert.Equal(originalReportId, media.SourceReportId);
    }
}
