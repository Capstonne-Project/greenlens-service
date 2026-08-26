using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// BR-CMP-007: Daily job that:
/// 1. Auto-expires Bidding companies past ContractEndDate → Status = Expired + cascading.
/// 2. Sends warning notifications at 30/7/1 day(s) before expiry to DEO and CM.
/// Subsidiary companies (vô thời hạn) are skipped entirely.
/// Runs at 2:00 AM UTC daily.
/// </summary>
/// <remarks>Implements: BR-CMP-007, BR-CMP-013, BR-NTF-002.</remarks>
[AutomaticRetry(Attempts = 2)]
internal sealed class CompanyContractExpiryJob(
    ApplicationDbContext db,
    IEnvironmentalServiceCompanyRepository companies,
    ICompanyCascadeService cascadeService,
    INotificationService notificationService,
    ISystemSettingsProvider systemSettings,
    ILogger<CompanyContractExpiryJob> logger)
{
    public async Task ExecuteAsync()
    {
        logger.LogInformation("CompanyContractExpiryJob: Starting...");

        var today = DateTime.UtcNow.Date;

        var expiredCompanies = await companies
            .GetBiddingExpiredAsync(today, CancellationToken.None)
            .ConfigureAwait(false);

        foreach (var company in expiredCompanies)
        {
            company.Expire();

            await cascadeService.CascadeDeactivationAsync(
                company.Id,
                "Hợp đồng hết hạn (auto-expire)",
                CancellationToken.None).ConfigureAwait(false);

            var cmId = await db.CompanyStaff
                .AsNoTracking()
                .Where(cs => cs.CompanyId == company.Id)
                .Join(db.Users, cs => cs.UserId, u => u.Id, (cs, u) => new { cs, u })
                .Where(x => x.u.Role == UserRole.CompanyManager)
                .Select(x => x.cs.UserId)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (cmId != Guid.Empty)
            {
                await notificationService.SendFromTemplateAsync(
                    cmId,
                    NotificationType.ContractExpired,
                    JobNotificationPlaceholders.ForContractExpired(
                        company.Name,
                        company.ContractNumber ?? "N/A"),
                    company.Id).ConfigureAwait(false);
            }

            logger.LogWarning("CompanyContractExpiryJob: Expired company {CompanyId} ({Name})",
                company.Id, company.Name);
        }

        var warningDays = ModuleSystemSettings.ContractWarningDays(systemSettings);
        foreach (var days in warningDays)
        {
            var fromDate = today.AddDays(days);
            var toDate = today.AddDays(days + 1);

            var warningCompanies = await companies
                .GetBiddingExpiringBetweenAsync(fromDate, toDate, CancellationToken.None)
                .ConfigureAwait(false);

            foreach (var company in warningCompanies)
            {
                if (company.LastExpiryWarningAt.HasValue
                    && company.LastExpiryWarningAt.Value.Date == today)
                    continue;

                await SendExpiryWarningAsync(company, days).ConfigureAwait(false);

                await db.Database.ExecuteSqlRawAsync(
                    "UPDATE environmental_service_companies SET last_expiry_warning_at = {0}, updated_at = {0} WHERE id = {1}",
                    DateTime.UtcNow, company.Id).ConfigureAwait(false);
            }
        }

        await db.SaveChangesAsync().ConfigureAwait(false);

        logger.LogInformation(
            "CompanyContractExpiryJob: Completed. Expired {ExpiredCount} companies, sent warnings.",
            expiredCompanies.Count);
    }

    private async Task SendExpiryWarningAsync(EnvironmentalServiceCompany company, int daysLeft)
    {
        var endDate = company.ContractEndDate?.ToString("dd/MM/yyyy") ?? "N/A";
        var placeholders = JobNotificationPlaceholders.ForContractExpiryWarning(
            company.Name,
            daysLeft,
            endDate);

        var deoId = await db.Users
            .AsNoTracking()
            .Where(u => u.DepartmentId == company.DepartmentId && u.Role == UserRole.DEO && !u.IsBanned)
            .Select(u => u.Id)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (deoId != Guid.Empty)
        {
            await notificationService.SendFromTemplateAsync(
                deoId,
                NotificationType.ContractExpiryWarning,
                placeholders,
                company.Id).ConfigureAwait(false);
        }

        var cmId = await db.CompanyStaff
            .AsNoTracking()
            .Where(cs => cs.CompanyId == company.Id)
            .Join(db.Users, cs => cs.UserId, u => u.Id, (cs, u) => new { cs, u })
            .Where(x => x.u.Role == UserRole.CompanyManager)
            .Select(x => x.cs.UserId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (cmId != Guid.Empty)
        {
            await notificationService.SendFromTemplateAsync(
                cmId,
                NotificationType.ContractExpiryWarning,
                placeholders,
                company.Id).ConfigureAwait(false);
        }

        logger.LogInformation(
            "CompanyContractExpiryJob: Sent {Days}-day warning for company {CompanyId} ({Name})",
            daysLeft, company.Id, company.Name);
    }
}
