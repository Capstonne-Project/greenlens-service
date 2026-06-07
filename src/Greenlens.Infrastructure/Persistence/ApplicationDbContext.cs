using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence;

internal sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    // ── Report module ──
    public DbSet<PollutionCategory> PollutionCategories => Set<PollutionCategory>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportMedia> ReportMedia => Set<ReportMedia>();
    public DbSet<ReportStatusHistory> ReportStatusHistory => Set<ReportStatusHistory>();
    public DbSet<ReportFlag> ReportFlags => Set<ReportFlag>();
    public DbSet<ReportSatisfaction> ReportSatisfactions => Set<ReportSatisfaction>();
    public DbSet<ReportDraft> ReportDrafts => Set<ReportDraft>();
    public DbSet<ReportAssignment> ReportAssignments => Set<ReportAssignment>();
    public DbSet<WasteTag> WasteTags => Set<WasteTag>();
    public DbSet<ReportWasteTag> ReportWasteTags => Set<ReportWasteTag>();

    // ── Organization module (v1.1) ──
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<LocalOffice> LocalOffices => Set<LocalOffice>();
    public DbSet<EnvironmentalTeam> EnvironmentalTeams => Set<EnvironmentalTeam>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    // ── Inspection & Company module (v1.3) ──
    public DbSet<InspectionReport> InspectionReports => Set<InspectionReport>();
    public DbSet<EnvironmentalServiceCompany> EnvironmentalServiceCompanies => Set<EnvironmentalServiceCompany>();
    public DbSet<CompanyStaff> CompanyStaff => Set<CompanyStaff>();
    public DbSet<CompanyServiceArea> CompanyServiceAreas => Set<CompanyServiceArea>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
