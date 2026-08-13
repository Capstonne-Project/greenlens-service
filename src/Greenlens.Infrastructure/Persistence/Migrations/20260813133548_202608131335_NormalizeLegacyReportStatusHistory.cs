using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _202608131335_NormalizeLegacyReportStatusHistory : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // v1.3 removed Dispatched/Assigned/PenaltyIssued/ClosedNoViolation from ReportStatus.
        migrationBuilder.Sql("""
            UPDATE report_status_history
            SET to_status = 'InProgress'
            WHERE to_status IN ('Dispatched', 'Assigned', 'PenaltyIssued');

            UPDATE report_status_history
            SET from_status = 'InProgress'
            WHERE from_status IN ('Dispatched', 'Assigned', 'PenaltyIssued');

            UPDATE report_status_history
            SET to_status = 'Closed'
            WHERE to_status = 'ClosedNoViolation';

            UPDATE report_status_history
            SET from_status = 'Closed'
            WHERE from_status = 'ClosedNoViolation';

            UPDATE reports
            SET status = 'InProgress'
            WHERE status IN ('Dispatched', 'Assigned', 'PenaltyIssued');

            UPDATE reports
            SET status = 'Closed'
            WHERE status = 'ClosedNoViolation';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Irreversible data normalization — legacy enum values are no longer supported.
    }
}
