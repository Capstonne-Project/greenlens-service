using FluentAssertions;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Reports.GetReportById;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Greenlens.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class GetReportByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_IncludesReopenAndAssignmentHistory_BR_REP_015()
    {
        await using var ctx = CreateDb();

        var citizen = User.Create("citizen@test.local", "hash", "Citizen User", UserRole.Citizen);
        var leo = User.Create("leo@test.local", "hash", "LEO User", UserRole.LEO);
        var category = PollutionCategory.Create("waste", "Rác thải", "Waste");
        var team = EnvironmentalTeam.Create("Cleanup A", Guid.NewGuid(), TeamType.Cleanup);

        ctx.Users.AddRange(citizen, leo);
        ctx.PollutionCategories.Add(category);
        ctx.EnvironmentalTeams.Add(team);
        await ctx.SaveChangesAsync();

        var report = Report.Create(
            "RPT-HIST",
            citizen.Id,
            category.Id,
            Severity.Medium,
            "Test",
            10.7626m,
            106.6602m,
            null,
            null,
            null);
        report.Verify(leo.Id);
        report.Assign(leo.Id);

        var oldCompleted = ReportAssignment.Create(report.Id, team.Id, leo.Id);
        oldCompleted.Accept();
        oldCompleted.Complete();
        SetAssignedAt(oldCompleted, DateTime.UtcNow.AddDays(-2));

        report.Resolve();
        report.ApproveReopen(leo.Id);
        report.Assign(leo.Id);

        var newAssigned = ReportAssignment.Create(report.Id, team.Id, leo.Id);
        SetAssignedAt(newAssigned, DateTime.UtcNow);

        var approvedReopen = ReportReopenRequest.Create(report.Id, citizen.Id, "Vẫn còn rác sau khi dọn.");
        approvedReopen.Approve(leo.Id);

        ctx.Reports.Add(report);
        ctx.ReportAssignments.AddRange(oldCompleted, newAssigned);
        ctx.ReportReopenRequests.Add(approvedReopen);
        await ctx.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(citizen.Id);
        currentUser.Role.Returns(UserRole.Citizen.ToString());

        var sut = new GetReportByIdQueryHandler(
            new ReportRepository(ctx),
            new ReportMediaRepository(ctx),
            new ReportSatisfactionRepository(ctx),
            Substitute.For<IInspectionReportRepository>(),
            ctx,
            currentUser,
            NullLogger<GetReportByIdQueryHandler>.Instance);

        var result = await sut.Handle(new GetReportByIdQuery(report.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReopenHistory.Should().HaveCount(1);
        result.Value.ReopenHistory[0].Status.Should().Be(ReopenRequestStatus.Approved.ToString());
        result.Value.ReopenHistory[0].ReviewedByName.Should().Be("LEO User");

        result.Value.AssignmentHistory.Should().HaveCount(2);
        result.Value.AssignmentHistory[0].AssignmentId.Should().Be(newAssigned.Id);
        result.Value.AssignmentHistory[0].IsCurrent.Should().BeTrue();
        result.Value.AssignmentHistory[1].AssignmentId.Should().Be(oldCompleted.Id);
        result.Value.AssignmentHistory[1].IsCurrent.Should().BeFalse();

        result.Value.CurrentAssignment.Should().NotBeNull();
        result.Value.CurrentAssignment!.Id.Should().Be(newAssigned.Id);
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"get-report-by-id-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static void SetAssignedAt(ReportAssignment assignment, DateTime assignedAt)
    {
        typeof(ReportAssignment)
            .GetProperty(nameof(ReportAssignment.AssignedAt))!
            .SetValue(assignment, assignedAt);
    }
}
