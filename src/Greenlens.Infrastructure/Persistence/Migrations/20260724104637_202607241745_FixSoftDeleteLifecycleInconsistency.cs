using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations;

/// <summary>
/// Data fix: align lifecycle flags with soft-delete for rows archived before guard rules existed.
/// BR-CMP-004: archived companies must not remain Active/Suspended/PendingActivation.
/// </summary>
public partial class _202607241745_FixSoftDeleteLifecycleInconsistency : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE environmental_service_companies
            SET status = 'Terminated',
                updated_at = NOW() AT TIME ZONE 'UTC'
            WHERE deleted_at IS NOT NULL
              AND status IN ('Active', 'Suspended', 'PendingActivation');
            """);

        migrationBuilder.Sql("""
            UPDATE environmental_teams
            SET is_active = false,
                updated_at = NOW() AT TIME ZONE 'UTC'
            WHERE deleted_at IS NOT NULL
              AND is_active = true;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Irreversible data correction — previous status/is_active values were inconsistent.
    }
}
