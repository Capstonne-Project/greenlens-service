using FluentAssertions;
using Greenlens.Application.Features.Reports.GetDuplicateCandidatesV2;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Greenlens.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Greenlens.Application.UnitTests;

public sealed class GetDuplicateCandidatesV2QueryHandlerTests
{
    [Fact]
    public async Task Handle_GroupsDuplicatesByPrimary_BR_REP_031()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"dup-candidates-v2-{Guid.NewGuid():N}")
            .Options;

        var ctx = new ApplicationDbContext(options);
        var category = PollutionCategory.Create("TRASH", "Rác thải", "Trash");
        ctx.PollutionCategories.Add(category);
        await ctx.SaveChangesAsync();

        var primary = CreateReport("RPT-PRIMARY", category.Id, 10.7626m, 106.6602m);
        var dup1 = CreateReport("RPT-DUP-1", category.Id, 10.7627m, 106.6603m);
        var dup2 = CreateReport("RPT-DUP-2", category.Id, 10.7628m, 106.6604m);

        dup1.MarkPossibleDuplicate(primary.Id, DuplicateDetectionSources.Tier1);
        dup2.MarkPossibleDuplicate(primary.Id, DuplicateDetectionSources.Tier2Ai);

        ctx.Reports.AddRange(primary, dup1, dup2);
        await ctx.SaveChangesAsync();

        var sut = new GetDuplicateCandidatesV2QueryHandler(
            new ReportRepository(ctx),
            new ReportMediaRepository(ctx),
            NullLogger<GetDuplicateCandidatesV2QueryHandler>.Instance);

        var result = await sut.Handle(new GetDuplicateCandidatesV2Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Primary.Code.Should().Be("RPT-PRIMARY");
        result.Value.Items[0].DuplicateCount.Should().Be(2);
        result.Value.Items[0].Duplicates.Should().HaveCount(2);
        result.Value.Items[0].Duplicates.Select(d => d.Code).Should().BeEquivalentTo(["RPT-DUP-1", "RPT-DUP-2"]);
        result.Value.Pagination.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task Handle_PrimaryReportIdFilter_ReturnsSingleGroup_BR_REP_031()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"dup-candidates-v2-filter-{Guid.NewGuid():N}")
            .Options;

        var ctx = new ApplicationDbContext(options);
        var category = PollutionCategory.Create("TRASH", "Rác thải", "Trash");
        ctx.PollutionCategories.Add(category);
        await ctx.SaveChangesAsync();

        var primary1 = CreateReport("RPT-P1", category.Id, 10.7626m, 106.6602m);
        var primary2 = CreateReport("RPT-P2", category.Id, 10.7700m, 106.6700m);
        var dupForP1 = CreateReport("RPT-D-P1", category.Id, 10.7627m, 106.6603m);
        var dupForP2 = CreateReport("RPT-D-P2", category.Id, 10.7701m, 106.6701m);

        dupForP1.MarkPossibleDuplicate(primary1.Id, DuplicateDetectionSources.Tier1);
        dupForP2.MarkPossibleDuplicate(primary2.Id, DuplicateDetectionSources.Tier1);

        ctx.Reports.AddRange(primary1, primary2, dupForP1, dupForP2);
        await ctx.SaveChangesAsync();

        var sut = new GetDuplicateCandidatesV2QueryHandler(
            new ReportRepository(ctx),
            new ReportMediaRepository(ctx),
            NullLogger<GetDuplicateCandidatesV2QueryHandler>.Instance);

        var result = await sut.Handle(
            new GetDuplicateCandidatesV2Query(PrimaryReportId: primary1.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Primary.Id.Should().Be(primary1.Id);
        result.Value.Items[0].Duplicates.Should().ContainSingle(d => d.Code == "RPT-D-P1");
        result.Value.Pagination.TotalItems.Should().Be(1);
    }

    private static Report CreateReport(string code, Guid categoryId, decimal lat, decimal lng) =>
        Report.Create(
            code,
            reporterId: Guid.NewGuid(),
            categoryId,
            severity: Severity.Medium,
            description: "Test duplicate candidate",
            latitude: lat,
            longitude: lng,
            address: "Test address",
            wardCode: "00001",
            provinceCode: "79");
}
