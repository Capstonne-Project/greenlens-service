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

    // ── Organization module (v1.1) ──
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<LocalOffice> LocalOffices => Set<LocalOffice>();
    public DbSet<EnvironmentalTeam> EnvironmentalTeams => Set<EnvironmentalTeam>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
