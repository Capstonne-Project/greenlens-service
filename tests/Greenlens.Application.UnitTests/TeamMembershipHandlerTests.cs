using FluentAssertions;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Organization.AddCompanyTeamMember;
using Greenlens.Application.Features.Organization.AddTeamMember;
using Greenlens.Application.Features.Organization.RemoveCompanyTeamMember;
using Greenlens.Application.Features.Organization.RemoveTeamMember;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Greenlens.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class TeamMembershipHandlerTests
{
    [Fact]
    public async Task RemoveTeamMember_WhenTeamHasActiveCleanupTask_ReturnsCannotModify_BR_ORG_003()
    {
        await using var ctx = CreateDb();
        var officeId = Guid.NewGuid();
        var team = EnvironmentalTeam.Create("Cleanup A", officeId, TeamType.Cleanup);
        var userId = Guid.NewGuid();
        var member = TeamMember.Create(team.Id, userId, isLeader: true);

        var category = PollutionCategory.Create("TRASH", "Rác thải", "Trash");
        var report = Report.Create(
            "RPT-1", Guid.NewGuid(), category.Id, Severity.Medium, "Test",
            10.7626m, 106.6602m, null, null, null);
        var assignment = ReportAssignment.Create(report.Id, team.Id, Guid.NewGuid());

        ctx.PollutionCategories.Add(category);
        ctx.EnvironmentalTeams.Add(team);
        ctx.TeamMembers.Add(member);
        ctx.Reports.Add(report);
        ctx.ReportAssignments.Add(assignment);
        await ctx.SaveChangesAsync();

        var sut = new RemoveTeamMemberCommandHandler(
            new TeamMemberRepository(ctx),
            new ReportAssignmentRepository(ctx),
            new InspectionReportRepository(ctx),
            Substitute.For<IUnitOfWork>(),
            NullLogger<RemoveTeamMemberCommandHandler>.Instance);

        var result = await sut.Handle(
            new RemoveTeamMemberCommand(team.Id, userId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CANNOT_MODIFY_TEAM_WITH_ACTIVE_TASKS");
    }

    [Fact]
    public async Task RemoveTeamMember_WhenNoActiveTasks_RemovesMember_BR_ORG_003()
    {
        await using var ctx = CreateDb();
        var officeId = Guid.NewGuid();
        var team = EnvironmentalTeam.Create("Cleanup B", officeId, TeamType.Cleanup);
        var userId = Guid.NewGuid();
        var member = TeamMember.Create(team.Id, userId);

        ctx.EnvironmentalTeams.Add(team);
        ctx.TeamMembers.Add(member);
        await ctx.SaveChangesAsync();

        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => ctx.SaveChangesAsync(callInfo.Arg<CancellationToken>()));
        var sut = new RemoveTeamMemberCommandHandler(
            new TeamMemberRepository(ctx),
            new ReportAssignmentRepository(ctx),
            new InspectionReportRepository(ctx),
            uow,
            NullLogger<RemoveTeamMemberCommandHandler>.Instance);

        var result = await sut.Handle(
            new RemoveTeamMemberCommand(team.Id, userId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await ctx.TeamMembers.CountAsync()).Should().Be(0);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveCompanyTeamMember_WhenTeamHasActiveTask_ReturnsCannotModify_BR_CMP_004()
    {
        await using var ctx = CreateDb();
        var companyId = Guid.NewGuid();
        var team = EnvironmentalTeam.CreateCompanyTeam("Company Cleanup", TeamType.Cleanup, companyId);
        var cmId = Guid.NewGuid();
        var staffUserId = Guid.NewGuid();
        var member = TeamMember.Create(team.Id, staffUserId, isLeader: true);

        var category = PollutionCategory.Create("TRASH", "Rác thải", "Trash");
        var report = Report.Create(
            "RPT-CMP", Guid.NewGuid(), category.Id, Severity.Medium, "Test",
            10.7626m, 106.6602m, null, null, null);
        var assignment = ReportAssignment.Create(report.Id, team.Id, Guid.NewGuid());

        ctx.PollutionCategories.Add(category);
        ctx.EnvironmentalTeams.Add(team);
        ctx.TeamMembers.Add(member);
        ctx.Reports.Add(report);
        ctx.ReportAssignments.Add(assignment);
        ctx.CompanyStaff.Add(CompanyStaff.Create(cmId, companyId));
        await ctx.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(cmId);

        var sut = new RemoveCompanyTeamMemberCommandHandler(
            new CompanyStaffRepository(ctx),
            new EnvironmentalTeamRepository(ctx),
            new TeamMemberRepository(ctx),
            new ReportAssignmentRepository(ctx),
            new InspectionReportRepository(ctx),
            Substitute.For<IUnitOfWork>(),
            currentUser,
            NullLogger<RemoveCompanyTeamMemberCommandHandler>.Instance);

        var result = await sut.Handle(
            new RemoveCompanyTeamMemberCommand(team.Id, staffUserId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CANNOT_MODIFY_TEAM_WITH_ACTIVE_TASKS");
    }

    [Fact]
    public async Task AddTeamMember_WhenTeamAlreadyHasLeader_ReturnsTeamAlreadyHasLeader_BR_ORG_003()
    {
        await using var ctx = CreateDb();
        var officeId = Guid.NewGuid();
        var team = EnvironmentalTeam.Create("Cleanup C", officeId, TeamType.Cleanup);
        var leo = User.CreateByAdmin("leo@test.com", "hash", "LEO", UserRole.LEO);
        leo.AssignToLocalOffice(officeId);

        var existingLeader = User.CreateByAdmin("leader@test.com", "hash", "Leader", UserRole.Cleaner);
        existingLeader.AssignToLocalOffice(officeId);

        var newCleaner = User.CreateByAdmin("cleaner@test.com", "hash", "Cleaner", UserRole.Cleaner);
        newCleaner.AssignToLocalOffice(officeId);

        ctx.EnvironmentalTeams.Add(team);
        ctx.Users.AddRange(leo, existingLeader, newCleaner);
        ctx.TeamMembers.Add(TeamMember.Create(team.Id, existingLeader.Id, isLeader: true));
        await ctx.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(leo.Id);

        var sut = new AddTeamMemberCommandHandler(
            new EnvironmentalTeamRepository(ctx),
            new TeamMemberRepository(ctx),
            new UserRepository(ctx),
            currentUser,
            Substitute.For<IUnitOfWork>(),
            NullLogger<AddTeamMemberCommandHandler>.Instance);

        var result = await sut.Handle(
            new AddTeamMemberCommand(team.Id, newCleaner.Id, IsLeader: true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("TEAM_ALREADY_HAS_LEADER");
        result.Error.Type.Should().Be(ErrorType.BusinessRule);
    }

    [Fact]
    public async Task AddCompanyTeamMember_WhenTeamAlreadyHasLeader_ReturnsTeamAlreadyHasLeader_BR_CMP_004()
    {
        await using var ctx = CreateDb();
        var companyId = Guid.NewGuid();
        var cmId = Guid.NewGuid();
        var existingLeaderId = Guid.NewGuid();
        var team = EnvironmentalTeam.CreateCompanyTeam("Company Team", TeamType.Cleanup, companyId);
        var newStaff = User.CreateByAdmin("staff@test.com", "hash", "Staff", UserRole.CompanyStaff);

        ctx.EnvironmentalTeams.Add(team);
        ctx.Users.Add(newStaff);
        ctx.CompanyStaff.AddRange(
            CompanyStaff.Create(cmId, companyId),
            CompanyStaff.Create(existingLeaderId, companyId),
            CompanyStaff.Create(newStaff.Id, companyId));
        ctx.TeamMembers.Add(TeamMember.Create(team.Id, existingLeaderId, isLeader: true));
        await ctx.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(cmId);

        var sut = new AddCompanyTeamMemberCommandHandler(
            new CompanyStaffRepository(ctx),
            new EnvironmentalTeamRepository(ctx),
            new TeamMemberRepository(ctx),
            new UserRepository(ctx),
            Substitute.For<IUnitOfWork>(),
            currentUser,
            NullLogger<AddCompanyTeamMemberCommandHandler>.Instance);

        var result = await sut.Handle(
            new AddCompanyTeamMemberCommand(team.Id, newStaff.Id, IsLeader: true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("TEAM_ALREADY_HAS_LEADER");
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"team-membership-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
