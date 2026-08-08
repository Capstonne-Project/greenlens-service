using FluentAssertions;
using Greenlens.Application.Features.Reports.DeleteReport;
using Greenlens.Application.IntegrationTests.Fixtures;
using Greenlens.Application.IntegrationTests.Helpers;
using Greenlens.Domain.Common;

namespace Greenlens.Application.IntegrationTests.Features.SoftDelete;

[Collection("Postgres")]
public sealed class ReportSoftDeleteTests(PostgresContainerFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task DeleteReport_WhenAlreadyDeleted_ReturnsConflict_BR_REP_017()
    {
        var (reportId, reporterId) = await WithDbAsync(async db =>
        {
            var category = await IntegrationDataSeeder.SeedCategoryAsync(db, $"CAT-{Guid.NewGuid():N}"[..8]);
            var (reporter, report) = await IntegrationDataSeeder.SeedReportAsync(db, category, softDeleted: true);
            return (report.Id, reporter.Id);
        });

        CurrentUser.UserId = reporterId;
        CurrentUser.Role = "Citizen";

        var result = await Mediator.Send(new DeleteReportCommand(reportId));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("REPORT_ALREADY_DELETED");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }
}
