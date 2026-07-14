using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence;

internal sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ICurrentUser? currentUser = null)
    : DbContext(options), IApplicationDbContext
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
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<CommentMedia> CommentMedia => Set<CommentMedia>();

    // ── Organization module (v1.1) ──
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<LocalOffice> LocalOffices => Set<LocalOffice>();
    public DbSet<EnvironmentalTeam> EnvironmentalTeams => Set<EnvironmentalTeam>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    // ── Inspection & Company module (v1.3) ──
    public DbSet<InspectionReport> InspectionReports => Set<InspectionReport>();
    public DbSet<ViolatingEntity> ViolatingEntities => Set<ViolatingEntity>();
    public DbSet<PenaltyPayment> PenaltyPayments => Set<PenaltyPayment>();
    public DbSet<EnvironmentalServiceCompany> EnvironmentalServiceCompanies => Set<EnvironmentalServiceCompany>();
    public DbSet<CompanyStaff> CompanyStaff => Set<CompanyStaff>();
    public DbSet<CompanyServiceArea> CompanyServiceAreas => Set<CompanyServiceArea>();
    public DbSet<ContractPeriod> ContractPeriods => Set<ContractPeriod>();

    // ── Gamification module (v1.2) ──
    public DbSet<UserPoints> UserPoints => Set<UserPoints>();
    public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();

    // ── Notification module (BR-NTF-001..004) ──
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    // ── Password History (BR-AUTH-020) ──
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();

    // ── Staff Invitation (BR-ORG-021) ──
    public DbSet<StaffInvitation> StaffInvitations => Set<StaffInvitation>();

    // ── Administration module (BR-ADM-*) ──
    public DbSet<PenaltyFramework> PenaltyFrameworks => Set<PenaltyFramework>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<GamificationConfig> GamificationConfigs => Set<GamificationConfig>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<BlockedWord> BlockedWords => Set<BlockedWord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Automatically sets CreatedAt/UpdatedAt and CreatedBy/UpdatedBy
    /// on all AuditableEntity descendants before persisting.
    /// This is the centralized fix — no entity needs to set these manually.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var userId = currentUser is { IsAuthenticated: true }
            ? currentUser.UserId.ToString()
            : null;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.CreatedAt == default)
                        entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy ??= userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy ??= userId;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

