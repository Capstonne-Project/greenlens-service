using FluentAssertions;
using Greenlens.Application.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.UnitTests;

/// <summary>Verifies Tier 1 duplicate candidate query matches submit handler logic (BR-REP-030).</summary>
public sealed class Tier1DuplicateDetectionQueryTests
{
    private const string WardLongBinh = "26808";
    private const string WardHoangHuuNam = "26809";
    private const string ProvinceCode = "79";

    [Fact]
    public async Task Tier1Query_WhenSameCategoryWithin25m_FlagsPossibleDuplicate_BR_REP_030()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"tier1-dup-{Guid.NewGuid():N}")
            .Options;

        await using var ctx = new ApplicationDbContext(options);
        var category = PollutionCategory.Create("TRASH", "Rác thải", "Trash");
        ctx.PollutionCategories.Add(category);

        var officeId = Guid.NewGuid();
        var primary = CreateSubmittedReport(
            "RPT-PRIMARY", category.Id, officeId, 10.7626m, 106.6602m, WardLongBinh);
        primary.Verify(Guid.NewGuid());

        ctx.Reports.Add(primary);
        await ctx.SaveChangesAsync();

        var newReport = Report.Create(
            "RPT-NEW",
            Guid.NewGuid(),
            category.Id,
            Severity.Medium,
            "New duplicate",
            10.7627m,
            106.6603m,
            "Near primary",
            WardLongBinh,
            ProvinceCode);
        newReport.RouteToLocalOffice(officeId, Guid.NewGuid());

        var candidates = await QueryTier1CandidatesAsync(ctx, newReport);
        var primaryId = DuplicateTier1PrimarySelector.SelectPrimary(
            newReport.Latitude, newReport.Longitude, newReport.WardCode, newReport.ProvinceCode, candidates);

        primaryId.Should().Be(primary.Id);
    }

    [Fact]
    public async Task Tier1Query_WhenDifferentWardWithin25m_DoesNotFlag_BR_REP_030()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"tier1-ward-{Guid.NewGuid():N}")
            .Options;

        await using var ctx = new ApplicationDbContext(options);
        var category = PollutionCategory.Create("TRASH", "Rác thải", "Trash");
        ctx.PollutionCategories.Add(category);

        var officeId = Guid.NewGuid();
        var primary = CreateSubmittedReport(
            "RPT-LONG-BINH", category.Id, officeId, 10.7626m, 106.6602m, WardLongBinh);
        primary.Verify(Guid.NewGuid());

        ctx.Reports.Add(primary);
        await ctx.SaveChangesAsync();

        var newReport = Report.Create(
            "RPT-HOANG-HUU-NAM",
            Guid.NewGuid(),
            category.Id,
            Severity.Medium,
            "Opposite side of street",
            10.7627m,
            106.6603m,
            "Hoàng Hữu Nam street",
            WardHoangHuuNam,
            ProvinceCode);
        newReport.RouteToLocalOffice(Guid.NewGuid(), Guid.NewGuid());

        var candidates = await QueryTier1CandidatesAsync(ctx, newReport);
        DuplicateTier1PrimarySelector.SelectPrimary(
                newReport.Latitude, newReport.Longitude, newReport.WardCode, newReport.ProvinceCode, candidates)
            .Should().BeNull();
    }

    [Fact]
    public async Task Tier1Query_WhenPrimaryClosed_DoesNotFlag_BR_REP_030()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"tier1-closed-{Guid.NewGuid():N}")
            .Options;

        await using var ctx = new ApplicationDbContext(options);
        var category = PollutionCategory.Create("TRASH", "Rác thải", "Trash");
        ctx.PollutionCategories.Add(category);

        var closed = CreateSubmittedReport(
            "RPT-CLOSED", category.Id, Guid.NewGuid(), 10.7626m, 106.6602m, WardLongBinh);
        closed.ForceStatus(ReportStatus.Closed);

        ctx.Reports.Add(closed);
        await ctx.SaveChangesAsync();

        var newReport = Report.Create(
            "RPT-NEW",
            Guid.NewGuid(),
            category.Id,
            Severity.Medium,
            "After closed",
            10.7626m,
            106.6602m,
            "Same spot",
            WardLongBinh,
            ProvinceCode);

        var candidates = await QueryTier1CandidatesAsync(ctx, newReport);
        DuplicateTier1PrimarySelector.SelectPrimary(
                newReport.Latitude, newReport.Longitude, newReport.WardCode, newReport.ProvinceCode, candidates)
            .Should().BeNull();
    }

    private static Report CreateSubmittedReport(
        string code, Guid categoryId, Guid officeId, decimal lat, decimal lng, string wardCode)
    {
        var report = Report.Create(
            code,
            Guid.NewGuid(),
            categoryId,
            Severity.Medium,
            "Primary",
            lat,
            lng,
            "Address",
            wardCode,
            ProvinceCode);
        report.RouteToLocalOffice(officeId, Guid.NewGuid());
        return report;
    }

    private static async Task<List<DuplicateNearbyReport>> QueryTier1CandidatesAsync(
        ApplicationDbContext ctx, Report report)
    {
        if (!AdministrativeUnitMatch.HasWardAndProvince(report.WardCode, report.ProvinceCode))
            return [];

        const double radiusMeters = DuplicateTier1PrimarySelector.DefaultRadiusMeters;
        var latDelta = (decimal)(radiusMeters / 111_320.0);
        var cosLat = Math.Max(Math.Cos((double)report.Latitude * Math.PI / 180.0), 1e-6);
        var lngDelta = (decimal)(radiusMeters / (111_320.0 * cosLat));

        return await ctx.Reports.AsNoTracking()
            .Where(r => r.CategoryId == report.CategoryId)
            .Where(r => r.WardCode == report.WardCode && r.ProvinceCode == report.ProvinceCode)
            .Where(r => r.Id != report.Id)
            .Where(r => r.Status != ReportStatus.Duplicate
                     && r.Status != ReportStatus.Rejected
                     && r.Status != ReportStatus.Closed)
            .Where(r => r.Latitude >= report.Latitude - latDelta && r.Latitude <= report.Latitude + latDelta)
            .Where(r => r.Longitude >= report.Longitude - lngDelta && r.Longitude <= report.Longitude + lngDelta)
            .OrderByDescending(r =>
                r.Status == ReportStatus.Verified
                || r.Status == ReportStatus.InProgress
                || r.Status == ReportStatus.Reopened)
            .ThenBy(r => r.CreatedAt)
            .Select(r => new DuplicateNearbyReport(
                r.Id, r.Latitude, r.Longitude, r.WardCode, r.ProvinceCode, r.Status, r.CreatedAt))
            .Take(20)
            .ToListAsync()
            .ConfigureAwait(false);
    }
}
