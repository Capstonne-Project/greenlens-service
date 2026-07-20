using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Seeders;

/// <summary>
/// Seeds mobile QA accounts, demo company/teams, and sample reports for Inspector / CM / Cleaner flows.
/// Idempotent — skips when marker account exists.
/// </summary>
/// <remarks>Implements: mobile handoff P1 (SEED_ACCOUNTS.md).</remarks>
internal static class MobileDemoSeeder
{
    private const string MarkerEmail = "company@greenlens.dev";
    private const string DemoPassword = "Lualua123@";
    private const string DemoWardCode = "27145";
    private const string DemoProvinceCode = "79";

    /// <summary>All mobile QA emails (current + legacy) — password reset on every startup.</summary>
    private static readonly string[] DemoAccountEmails =
    [
        "citizen@greenlens.dev",
        "cleaner@greenlens.dev",
        "cleaner.member@greenlens.dev",
        "inspector@greenlens.dev",
        "company@greenlens.dev",
        "staff@greenlens.dev",
        // legacy (*.mobile@)
        "citizen.mobile@greenlens.dev",
        "cleaner.leader.mobile@greenlens.dev",
        "cleaner.member.mobile@greenlens.dev",
        "inspector.leader.mobile@greenlens.dev",
        "cm.mobile@greenlens.dev",
        "staff.leader.mobile@greenlens.dev"
    ];

    // HCMC Phường 1 — within BR-REP-003 bounds
    private const decimal DemoLat = 10.7769m;
    private const decimal DemoLng = 106.7009m;

    public static async Task SeedAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken ct = default)
    {
        await ResetDemoPasswordsAsync(db, logger, ct).ConfigureAwait(false);

        var alreadySeeded = await db.Users
            .AnyAsync(u => u.Email == MarkerEmail
                           || u.Email == "citizen@greenlens.dev"
                           || u.Email == "citizen.mobile@greenlens.dev", ct)
            .ConfigureAwait(false);

        if (alreadySeeded)
            return;

        var office = await db.LocalOffices
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.WardCode == DemoWardCode, ct)
            .ConfigureAwait(false);

        if (office is null)
        {
            logger.LogWarning("MobileDemoSeeder: ward {Ward} not found — skipping", DemoWardCode);
            return;
        }

        var department = await db.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.ProvinceCode == DemoProvinceCode, ct)
            .ConfigureAwait(false);

        if (department is null)
        {
            logger.LogWarning("MobileDemoSeeder: department {Province} not found — skipping", DemoProvinceCode);
            return;
        }

        var category = await db.PollutionCategories
            .AsNoTracking()
            .FirstAsync(c => c.Code == "TRASH", ct)
            .ConfigureAwait(false);

        var leo = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == $"leo.{DemoWardCode}@greenlens.dev", ct)
            .ConfigureAwait(false);

        if (leo is null)
        {
            logger.LogWarning("MobileDemoSeeder: LEO for ward {Ward} not found — skipping", DemoWardCode);
            return;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword, workFactor: 12);

        // ── Users (email đơn giản @greenlens.dev) ──
        var citizen = CreateVerifiedUser("citizen@greenlens.dev", passwordHash, "Demo Citizen", UserRole.Citizen);
        var cleanerLeader = CreateVerifiedUser("cleaner@greenlens.dev", passwordHash, "Demo Cleaner Leader", UserRole.Cleaner);
        var cleanerMember = CreateVerifiedUser("cleaner.member@greenlens.dev", passwordHash, "Demo Cleaner Member", UserRole.Cleaner);
        var inspectorLeader = CreateVerifiedUser("inspector@greenlens.dev", passwordHash, "Demo Inspector Leader", UserRole.Inspector);
        var cm = CreateVerifiedUser("company@greenlens.dev", passwordHash, "Demo Company Manager", UserRole.CompanyManager);
        var staffLeader = CreateVerifiedUser("staff@greenlens.dev", passwordHash, "Demo Staff Leader", UserRole.CompanyStaff);

        db.Users.AddRange(citizen, cleanerLeader, cleanerMember, inspectorLeader, cm, staffLeader);

        // ── Company (active, serves demo ward) ──
        var company = EnvironmentalServiceCompany.Create(
            name: "GreenLens Demo DVMT",
            departmentId: department.Id,
            contractNumber: "GL-DEMO-2026",
            contractStartDate: DateTime.UtcNow.AddMonths(-1),
            contractEndDate: null,
            contractType: ContractType.Subsidiary,
            taxCode: "0310000000",
            address: "Quận 1, TP.HCM",
            phone: "02812345678",
            email: "demo-dvmt@greenlens.dev");

        company.Activate();
        db.EnvironmentalServiceCompanies.Add(company);
        db.CompanyServiceAreas.Add(CompanyServiceArea.Create(company.Id, DemoWardCode));
        db.CompanyStaff.Add(CompanyStaff.Create(cm.Id, company.Id, "Manager"));
        db.CompanyStaff.Add(CompanyStaff.Create(staffLeader.Id, company.Id, "Field Leader"));

        // ── Teams ──
        var communityCleanupTeam = EnvironmentalTeam.Create(
            "Đội dọn cộng đồng Mobile Demo",
            office.Id,
            TeamType.Cleanup);

        var inspectionTeam = EnvironmentalTeam.Create(
            "Đội thanh tra Mobile Demo",
            office.Id,
            TeamType.Inspection);

        var companyCleanupTeam = EnvironmentalTeam.CreateCompanyTeam(
            "Đội công ty Mobile Demo",
            TeamType.Cleanup,
            company.Id);

        db.EnvironmentalTeams.AddRange(communityCleanupTeam, inspectionTeam, companyCleanupTeam);

        db.TeamMembers.AddRange(
            TeamMember.Create(communityCleanupTeam.Id, cleanerLeader.Id, isLeader: true),
            TeamMember.Create(communityCleanupTeam.Id, cleanerMember.Id),
            TeamMember.Create(inspectionTeam.Id, inspectorLeader.Id, isLeader: true),
            TeamMember.Create(companyCleanupTeam.Id, staffLeader.Id, isLeader: true));

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // ── Sample reports ──
        await SeedCompanyQueueReportAsync(db, citizen, category, office, department, leo, company, ct).ConfigureAwait(false);
        await SeedCompanyTaskReportAsync(db, citizen, category, office, department, leo, company, cm, companyCleanupTeam, ct).ConfigureAwait(false);
        await SeedCommunityTaskReportAsync(db, citizen, category, office, department, leo, communityCleanupTeam, ct).ConfigureAwait(false);
        await SeedInspectionReportAsync(db, citizen, category, office, department, leo, inspectionTeam, ct).ConfigureAwait(false);
        await SeedResolvedReportAsync(db, citizen, category, office, department, leo, communityCleanupTeam, ct).ConfigureAwait(false);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Mobile demo seeded: {Emails} / {Password} (ward {Ward})",
            string.Join(", ", DemoAccountEmails.Take(6)),
            DemoPassword,
            DemoWardCode);
    }

    /// <summary>Đồng bộ MK demo mỗi lần startup — fix login khi DB đã seed bản cũ.</summary>
    private static async Task ResetDemoPasswordsAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken ct)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword, workFactor: 12);
        var emails = DemoAccountEmails.Select(e => e.ToLowerInvariant()).ToArray();

        var users = await db.Users
            .Where(u => emails.Contains(u.Email))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (users.Count == 0)
            return;

        foreach (var user in users)
            user.ChangePassword(passwordHash);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Mobile demo passwords reset to {Password} for {Count} account(s)",
            DemoPassword,
            users.Count);
    }

    private static User CreateVerifiedUser(string email, string passwordHash, string fullName, UserRole role)
    {
        var user = User.CreateByAdmin(email, passwordHash, fullName, role);
        return user;
    }

    private static async Task SeedCompanyQueueReportAsync(
        ApplicationDbContext db,
        User citizen,
        PollutionCategory category,
        LocalOffice office,
        Department department,
        User leo,
        EnvironmentalServiceCompany company,
        CancellationToken ct)
    {
        if (await ReportExistsAsync(db, "REP-MOB-CQ001", ct).ConfigureAwait(false))
            return;

        var report = CreateBaseReport("REP-MOB-CQ001", citizen, category, "Báo cáo chờ CM phân công team");
        report.RouteToLocalOffice(office.Id, department.Id);
        report.Verify(leo.Id);
        report.DispatchToCompany(company.Id, leo.Id);
        db.Reports.Add(report);
    }

    private static async Task SeedCompanyTaskReportAsync(
        ApplicationDbContext db,
        User citizen,
        PollutionCategory category,
        LocalOffice office,
        Department department,
        User leo,
        EnvironmentalServiceCompany company,
        User cm,
        EnvironmentalTeam companyTeam,
        CancellationToken ct)
    {
        if (await ReportExistsAsync(db, "REP-MOB-TSK001", ct).ConfigureAwait(false))
            return;

        var report = CreateBaseReport("REP-MOB-TSK001", citizen, category, "Task công ty — InProgress cho staff leader");
        report.RouteToLocalOffice(office.Id, department.Id);
        report.Verify(leo.Id);
        report.DispatchToCompany(company.Id, leo.Id);
        report.AssignByCompanyManager(cm.Id);
        report.MarkStarted();

        var assignment = ReportAssignment.Create(report.Id, companyTeam.Id, cm.Id, "Mobile demo assignment");
        assignment.Accept();
        assignment.UpdateProgress(40, "Đang thu gom rác", cm.Id);

        db.Reports.Add(report);
        db.ReportAssignments.Add(assignment);
    }

    private static async Task SeedCommunityTaskReportAsync(
        ApplicationDbContext db,
        User citizen,
        PollutionCategory category,
        LocalOffice office,
        Department department,
        User leo,
        EnvironmentalTeam communityTeam,
        CancellationToken ct)
    {
        if (await ReportExistsAsync(db, "REP-MOB-CLN001", ct).ConfigureAwait(false))
            return;

        var report = CreateBaseReport("REP-MOB-CLN001", citizen, category, "Task cộng đồng — cleaner leader InProgress");
        report.RouteToLocalOffice(office.Id, department.Id);
        report.Verify(leo.Id);
        report.Assign(leo.Id);
        report.MarkStarted();

        var assignment = ReportAssignment.Create(report.Id, communityTeam.Id, leo.Id, "Mobile community cleanup");
        assignment.Accept();

        db.Reports.Add(report);
        db.ReportAssignments.Add(assignment);
    }

    private static async Task SeedInspectionReportAsync(
        ApplicationDbContext db,
        User citizen,
        PollutionCategory category,
        LocalOffice office,
        Department department,
        User leo,
        EnvironmentalTeam inspectionTeam,
        CancellationToken ct)
    {
        if (await ReportExistsAsync(db, "REP-MOB-INS001", ct).ConfigureAwait(false))
            return;

        var report = CreateBaseReport("REP-MOB-INS001", citizen, category, "Báo cáo có hồ sơ xử phạt Draft");
        report.RouteToLocalOffice(office.Id, department.Id);
        report.Verify(leo.Id);

        var inspection = InspectionReport.Create(
            report.Id,
            leo.Id,
            Severity.Medium,
            inspectionTeam.Id,
            violationDescription: "Phát hiện xả thải trái phép tại hiện trường",
            violatorName: "Cơ sở Demo XYZ",
            violatorAddress: "123 Nguyễn Huệ, Q1",
            violatorIdentity: "0310999999");

        db.Reports.Add(report);
        db.InspectionReports.Add(inspection);
    }

    private static async Task SeedResolvedReportAsync(
        ApplicationDbContext db,
        User citizen,
        PollutionCategory category,
        LocalOffice office,
        Department department,
        User leo,
        EnvironmentalTeam communityTeam,
        CancellationToken ct)
    {
        if (await ReportExistsAsync(db, "REP-MOB-RES001", ct).ConfigureAwait(false))
            return;

        var report = CreateBaseReport("REP-MOB-RES001", citizen, category, "Báo cáo Resolved — citizen close/reopen QA");
        report.RouteToLocalOffice(office.Id, department.Id);
        report.Verify(leo.Id);
        report.Assign(leo.Id);
        report.MarkStarted();
        report.Resolve();

        var assignment = ReportAssignment.Create(report.Id, communityTeam.Id, leo.Id);
        assignment.Accept();
        assignment.Complete();

        db.Reports.Add(report);
        db.ReportAssignments.Add(assignment);
    }

    private static Report CreateBaseReport(string code, User citizen, PollutionCategory category, string description)
        => Report.Create(
            code,
            citizen.Id,
            category.Id,
            Severity.Medium,
            description,
            DemoLat,
            DemoLng,
            "123 Nguyễn Huệ, Phường 1, TP.HCM",
            DemoWardCode,
            DemoProvinceCode);

    private static Task<bool> ReportExistsAsync(ApplicationDbContext db, string code, CancellationToken ct)
        => db.Reports.AnyAsync(r => r.Code == code, ct);
}
