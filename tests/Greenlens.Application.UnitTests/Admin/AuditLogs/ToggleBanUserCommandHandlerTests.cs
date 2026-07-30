using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Admin.ToggleBanUser;
using Greenlens.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests.Admin.AuditLogs;

public sealed class ToggleBanUserCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ToggleBanUserCommandHandler _sut;

    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid TargetUserId = Guid.NewGuid();

    public ToggleBanUserCommandHandlerTests()
    {
        _currentUser.UserId.Returns(AdminId);
        _sut = new ToggleBanUserCommandHandler(
            _users, _uow, _currentUser, NullLogger<ToggleBanUserCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_SelfBan_ReturnsCannotBanSelf_BR_ADM_010()
    {
        var result = await _sut.Handle(new ToggleBanUserCommand(AdminId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Users.CannotBanSelf.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ValidUser_TogglesBan_BR_ADM_010()
    {
        var user = User.Create("target@test.com", "hash", "Target User");
        _users.GetByIdAsync(TargetUserId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(new ToggleBanUserCommand(TargetUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsBanned);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
