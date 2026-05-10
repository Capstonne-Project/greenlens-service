using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Users.DeleteUser;
using Greenlens.Domain.Entities;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class DeleteUserCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly DeleteUserCommandHandler _sut;

    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid TargetUserId = Guid.NewGuid();

    public DeleteUserCommandHandlerTests()
    {
        _currentUser.UserId.Returns(AdminId);
        _currentUser.Email.Returns("admin@greenlens.com.vn");
        _sut = new DeleteUserCommandHandler(_users, _uow, _currentUser);
    }

    [Fact]
    public async Task Handle_ValidUser_ShouldSoftDelete()
    {
        var user = User.Create("target@test.com", "hash", "Target User");
        _users.GetByIdAsync(TargetUserId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(new DeleteUserCommand(TargetUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.IsDeleted);
        Assert.Equal("admin@greenlens.com.vn", user.DeletedBy);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeleteSelf_ShouldReturnError()
    {
        var result = await _sut.Handle(new DeleteUserCommand(AdminId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("CANNOT_DELETE_SELF", result.Error.Code);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnError()
    {
        _users.GetByIdAsync(TargetUserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.Handle(new DeleteUserCommand(TargetUserId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("USER_NOT_FOUND", result.Error.Code);
    }

    [Fact]
    public async Task Handle_DeleteSelf_ShouldNotCallRepository()
    {
        await _sut.Handle(new DeleteUserCommand(AdminId), CancellationToken.None);

        await _users.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
