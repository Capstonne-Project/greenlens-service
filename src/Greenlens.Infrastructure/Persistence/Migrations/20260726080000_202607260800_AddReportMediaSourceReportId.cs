using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Greenlens.Infrastructure.Persistence;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations;

/// <summary>
/// BR-REP-032: track origin report when media is reassigned during duplicate merge,
/// so FE can project per-child thumbnails on primary detail and my-reports.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260726080000_202607260800_AddReportMediaSourceReportId")]
public partial class _202607260800_AddReportMediaSourceReportId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "source_report_id",
            table: "report_media",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_report_media_source_report_id",
            table: "report_media",
            column: "source_report_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_report_media_source_report_id",
            table: "report_media");

        migrationBuilder.DropColumn(
            name: "source_report_id",
            table: "report_media");
    }
}
