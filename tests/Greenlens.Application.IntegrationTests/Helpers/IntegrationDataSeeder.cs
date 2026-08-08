using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Entities.Location;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.IntegrationTests.Helpers;

internal static class IntegrationDataSeeder
{
    public static async Task EnsureLocationCatalogAsync(IApplicationDbContext db)
    {
        if (await db.Set<Province>().AnyAsync(p => p.Code == "79"))
            return;

        db.Set<AdministrativeRegion>().Add(AdministrativeRegion.Seed(1, "Test Region"));
        db.Set<AdministrativeUnit>().Add(AdministrativeUnit.Seed(2, "Tỉnh", "Tỉnh"));
        db.Set<Province>().Add(Province.Seed("79", "TP HCM", 1, 2));
        await db.SaveChangesAsync();
    }

    public static async Task<Department> SeedDepartmentAsync(IApplicationDbContext db)
    {
        await EnsureLocationCatalogAsync(db);

        var existing = await db.Set<Department>()
            .FirstOrDefaultAsync(d => d.ProvinceCode == "79");
        if (existing is not null)
            return existing;

        var dept = Department.Create("Integration Dept", "79");
        db.Set<Department>().Add(dept);
        await db.SaveChangesAsync();
        return dept;
    }

    public static async Task<User> SeedUserAsync(IApplicationDbContext db, UserRole role = UserRole.Admin)
    {
        var user = User.CreateByAdmin(
            $"user-{Guid.NewGuid():N}@test.local",
            "hash",
            "Integration User",
            role);
        db.Set<User>().Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public static async Task<PollutionCategory> SeedCategoryAsync(
        IApplicationDbContext db,
        string code = "TRASH",
        bool softDeleted = false)
    {
        var category = PollutionCategory.Create(code, "Rác thải", "Trash");
        if (softDeleted)
            category.SoftDelete("seed");
        db.Set<PollutionCategory>().Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    public static async Task<WasteTag> SeedWasteTagAsync(
        IApplicationDbContext db,
        string code = "PLASTIC",
        bool softDeleted = false)
    {
        var tag = WasteTag.Create(code, "Nhựa", "Plastic");
        if (softDeleted)
            tag.SoftDelete("seed");
        db.Set<WasteTag>().Add(tag);
        await db.SaveChangesAsync();
        return tag;
    }

    public static async Task<(User reporter, Report report)> SeedReportAsync(
        IApplicationDbContext db,
        PollutionCategory category,
        bool softDeleted = false)
    {
        var reporter = await SeedUserAsync(db, UserRole.Citizen);
        var report = Report.Create(
            $"RPT-{Guid.NewGuid():N}"[..20],
            reporter.Id,
            category.Id,
            Severity.Medium,
            "Integration test report",
            10.7626m,
            106.6602m,
            "123 Test St",
            "00001",
            "79");

        if (softDeleted)
            report.SoftDelete("seed");

        db.Set<Report>().Add(report);
        await db.SaveChangesAsync();
        return (reporter, report);
    }

    public static async Task<EnvironmentalServiceCompany> SeedCompanyAsync(
        IApplicationDbContext db,
        CompanyStatus status = CompanyStatus.Active,
        bool softDeleted = false)
    {
        var dept = await SeedDepartmentAsync(db);
        var company = EnvironmentalServiceCompany.Create(
            "Integration Co",
            dept.Id,
            $"HD-{Guid.NewGuid():N}"[..12],
            DateTime.UtcNow.Date,
            null,
            ContractType.Subsidiary);

        if (status == CompanyStatus.Active)
            company.Activate();
        else if (status == CompanyStatus.Terminated)
        {
            company.Activate();
            company.Terminate();
        }

        if (softDeleted)
            company.Archive("seed", hasStaff: false);

        db.Set<EnvironmentalServiceCompany>().Add(company);
        await db.SaveChangesAsync();
        return company;
    }

    public static async Task<EnvironmentalTeam> SeedCompanyTeamAsync(
        IApplicationDbContext db,
        EnvironmentalServiceCompany company,
        bool softDeleted = false)
    {
        var team = EnvironmentalTeam.CreateCompanyTeam("Integration Team", TeamType.Cleanup, company.Id);
        if (softDeleted)
            team.Archive("seed", hasActiveAssignments: false);

        db.Set<EnvironmentalTeam>().Add(team);
        await db.SaveChangesAsync();
        return team;
    }

    public static async Task SeedInProgressAssignmentAsync(
        IApplicationDbContext db,
        EnvironmentalTeam team,
        PollutionCategory category)
    {
        var officer = await SeedUserAsync(db, UserRole.LEO);
        var (_, report) = await SeedReportAsync(db, category);
        var assignment = ReportAssignment.Create(report.Id, team.Id, officer.Id);
        assignment.Accept();
        db.Set<ReportAssignment>().Add(assignment);
        await db.SaveChangesAsync();
    }

    public static async Task<ViolatingEntity> SeedViolatingEntityAsync(
        IApplicationDbContext db,
        bool softDeleted = false)
    {
        var entity = ViolatingEntity.Create(
            "Integration Violator",
            ViolatorType.Business,
            taxCode: $"T{Guid.NewGuid():N}"[..10]);

        if (softDeleted)
            entity.SoftDelete("seed");

        db.Set<ViolatingEntity>().Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public static async Task<Ward> SeedWardAsync(IApplicationDbContext db, string? code = null)
    {
        await EnsureLocationCatalogAsync(db);
        code ??= $"W{Guid.NewGuid():N}"[..5];
        var ward = Ward.Seed(code, "Integration Ward", "79", 2);
        db.Set<Ward>().Add(ward);
        await db.SaveChangesAsync();
        return ward;
    }

    public static async Task<LocalOffice> SeedLocalOfficeAsync(IApplicationDbContext db)
    {
        var dept = await SeedDepartmentAsync(db);
        var ward = await SeedWardAsync(db);
        var office = LocalOffice.Create("Integration Office", dept.Id, ward.Code);
        db.Set<LocalOffice>().Add(office);
        await db.SaveChangesAsync();
        return office;
    }

    public static async Task<EnvironmentalServiceCompany> SeedBiddingCompanyAsync(
        IApplicationDbContext db,
        string contractNumber,
        CompanyStatus status = CompanyStatus.Active)
    {
        var dept = await SeedDepartmentAsync(db);
        var company = EnvironmentalServiceCompany.Create(
            "Bidding Co",
            dept.Id,
            contractNumber,
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddYears(1),
            ContractType.Bidding);

        if (status == CompanyStatus.Active)
            company.Activate();

        db.Set<EnvironmentalServiceCompany>().Add(company);
        await db.SaveChangesAsync();
        return company;
    }

    public static async Task SeedInspectionForEntityAsync(
        IApplicationDbContext db,
        ViolatingEntity entity,
        PollutionCategory category)
    {
        var officer = await SeedUserAsync(db, UserRole.LEO);
        var (_, report) = await SeedReportAsync(db, category);
        var inspection = InspectionReport.Create(report.Id, officer.Id, Severity.Medium);
        var linkResult = inspection.LinkViolatingEntity(entity.Id);
        if (!linkResult.IsSuccess)
            throw new InvalidOperationException(linkResult.Error!.Message);

        db.Set<InspectionReport>().Add(inspection);
        await db.SaveChangesAsync();
    }
}
