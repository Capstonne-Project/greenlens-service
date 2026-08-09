using FluentAssertions;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Notifications;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class CompanyTeamAssignedLeoNotifierTests
{
    [Fact]
    public async Task NotifyAsync_SendsCompanyTeamAssignedToOfficeLeos_BR_CMP_005()
    {
        await using var ctx = CreateDb();
        var officeId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var leo = User.CreateByAdmin("leo@test.com", "hash", "LEO", UserRole.LEO);
        leo.AssignToLocalOffice(officeId);

        var category = PollutionCategory.Create("TRASH", "Rác thải", "Trash");
        var report = Report.Create(
            "RPT-CMP-ASSIGN",
            Guid.NewGuid(),
            category.Id,
            Severity.Medium,
            "Test",
            10.7626m,
            106.6602m,
            null,
            "00001",
            "79");
        report.RouteToLocalOffice(officeId, Guid.NewGuid());
        report.Verify(leo.Id);
        report.DispatchToCompany(companyId, leo.Id);

        ctx.Users.Add(leo);
        ctx.PollutionCategories.Add(category);
        ctx.Reports.Add(report);
        await ctx.SaveChangesAsync();

        var company = EnvironmentalServiceCompany.Create(
            "Green Clean Co",
            Guid.NewGuid(),
            "GC-001",
            DateTime.UtcNow,
            DateTime.UtcNow.AddYears(1),
            ContractType.Bidding);

        var companies = Substitute.For<IEnvironmentalServiceCompanyRepository>();
        companies.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(company);

        var notificationService = Substitute.For<INotificationService>();
        var sut = new CompanyTeamAssignedLeoNotifier(
            notificationService,
            new OfficerRecipientQuery(ctx),
            companies,
            ctx,
            NullLogger<CompanyTeamAssignedLeoNotifier>.Instance);

        await sut.NotifyAsync(
            report.Id,
            report.Code,
            report.AssignedOfficeId,
            companyId,
            ["Đội Dọn Xanh 1"],
            CancellationToken.None);

        await notificationService.Received(1).SendFromTemplateAsync(
            leo.Id,
            NotificationType.CompanyTeamAssigned,
            Arg.Is<Dictionary<string, string>>(p =>
                p["report_code"] == "RPT-CMP-ASSIGN"
                && p["company_name"] == "Green Clean Co"
                && p["team_names"] == "Đội Dọn Xanh 1"),
            report.Id,
            Arg.Any<CancellationToken>());
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"company-team-assigned-leo-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
