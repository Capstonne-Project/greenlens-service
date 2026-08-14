using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBoundaryGeometryToLocationCatalog : Migration
    {
        /// <inheritdoc />
        /// <remarks>
        /// BR-ORG-004, BR-ORG-010, BR-ORG-016: point-in-polygon để xác định WardCode từ GPS.
        /// Cột geometry KHÔNG map vào EF model (Domain/Configuration không đổi) — chỉ truy cập
        /// qua raw SQL trong <see cref="Geo.WardBoundaryLookupService"/>. Dữ liệu polygon được
        /// import riêng qua tools/Greenlens.DbSeed (BoundaryGeometryImporter), không trong migration này.
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE provinces ADD COLUMN boundary geometry(MultiPolygon, 4326);
                ALTER TABLE wards ADD COLUMN boundary geometry(MultiPolygon, 4326);
                CREATE INDEX ix_provinces_boundary ON provinces USING GIST (boundary);
                CREATE INDEX ix_wards_boundary ON wards USING GIST (boundary);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS ix_wards_boundary;
                DROP INDEX IF EXISTS ix_provinces_boundary;
                ALTER TABLE wards DROP COLUMN IF EXISTS boundary;
                ALTER TABLE provinces DROP COLUMN IF EXISTS boundary;
                """);
        }
    }
}
