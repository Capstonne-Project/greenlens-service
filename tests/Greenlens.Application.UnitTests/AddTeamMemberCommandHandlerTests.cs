using FluentAssertions;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Organization.AddTeamMember;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class AddTeamMemberCommandHandlerTests
{
    private readonly IEnvironmentalTeamRepository _teams = Substitute.For<IEnvironmentalTeamRepository>();
    private readonly ITeamMemberRepository _teamMembers = Substitute.For<ITeamMemberRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly AddTeamMemberCommandHandler _sut;

    public AddTeamMemberCommandHandlerTests()
    {
        _sut = new AddTeamMemberCommandHandler(
            _teams,
            _teamMembers,
            _users,
            _currentUser,
            _uow,
            NullLogger<AddTeamMemberCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_UserAlreadyInAnotherTeam_ReturnsUserAlreadyInTeam_BR_ORG_003()
    {
        var officeId = Guid.NewGuid();
        var team = EnvironmentalTeam.Create("Cleanup A", officeId, TeamType.Cleanup);
        var otherTeamId = Guid.NewGuid();
        var leo = User.CreateByAdmin("leo@test.com", "hash", "LEO", UserRole.LEO);
        leo.AssignToLocalOffice(officeId);

        var cleaner = User.CreateByAdmin("cleaner@test.com", "hash", "Cleaner", UserRole.Cleaner);
        cleaner.AssignToLocalOffice(officeId);

        _currentUser.UserId.Returns(leo.Id);
        _teams.GetByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _users.GetByIdAsync(leo.Id, Arg.Any<CancellationToken>()).Returns(leo);
        _users.GetByIdAsync(cleaner.Id, Arg.Any<CancellationToken>()).Returns(cleaner);
        _teamMembers.GetByUserIdAsync(cleaner.Id, Arg.Any<CancellationToken>())
            .Returns(TeamMember.Create(otherTeamId, cleaner.Id));

        var result = await _sut.Handle(
            new AddTeamMemberCommand(team.Id, cleaner.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("USER_ALREADY_IN_TEAM");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_TeamOutsideLeoOffice_ReturnsTeamNotInOffice_BR_ORG_003()
    {
        var leoOfficeId = Guid.NewGuid();
        var otherOfficeId = Guid.NewGuid();
        var team = EnvironmentalTeam.Create("Cleanup B", otherOfficeId, TeamType.Cleanup);
        var leo = User.CreateByAdmin("leo@test.com", "hash", "LEO", UserRole.LEO);
        leo.AssignToLocalOffice(leoOfficeId);

        _currentUser.UserId.Returns(leo.Id);
        _teams.GetByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _users.GetByIdAsync(leo.Id, Arg.Any<CancellationToken>()).Returns(leo);

        var result = await _sut.Handle(
            new AddTeamMemberCommand(team.Id, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("TEAM_NOT_IN_OFFICE");
        result.Error.Type.Should().Be(ErrorType.BusinessRule);
    }

    [Fact]
    public async Task Handle_UserOutsideOffice_ReturnsUserNotInYourOffice_BR_ORG_003()
    {
        var officeId = Guid.NewGuid();
        var otherOfficeId = Guid.NewGuid();
        var team = EnvironmentalTeam.Create("Cleanup C", officeId, TeamType.Cleanup);
        var leo = User.CreateByAdmin("leo@test.com", "hash", "LEO", UserRole.LEO);
        leo.AssignToLocalOffice(officeId);

        var cleaner = User.CreateByAdmin("cleaner@test.com", "hash", "Cleaner", UserRole.Cleaner);
        cleaner.AssignToLocalOffice(otherOfficeId);

        _currentUser.UserId.Returns(leo.Id);
        _teams.GetByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _users.GetByIdAsync(leo.Id, Arg.Any<CancellationToken>()).Returns(leo);
        _users.GetByIdAsync(cleaner.Id, Arg.Any<CancellationToken>()).Returns(cleaner);
        _teamMembers.GetByUserIdAsync(cleaner.Id, Arg.Any<CancellationToken>()).Returns((TeamMember?)null);

        var result = await _sut.Handle(
            new AddTeamMemberCommand(team.Id, cleaner.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("USER_NOT_IN_YOUR_OFFICE");
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }
}
