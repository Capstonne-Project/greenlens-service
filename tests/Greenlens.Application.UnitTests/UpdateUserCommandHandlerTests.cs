using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Users.UpdateUser;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class UpdateUserCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly UpdateUserCommandHandler _sut;

    public UpdateUserCommandHandlerTests()
    {
        _sut = new UpdateUserCommandHandler(_users, _uow);
    }

    [Fact]
    public async Task Handle_ValidUser_ShouldUpdateAndSave()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("test@test.com", "hash", "Old Name");
        _users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(
            new UpdateUserCommand(userId, "New Name", "0901234567", UserRole.Officer, true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", user.FullName);
        Assert.Equal("0901234567", user.PhoneNumber);
        Assert.Equal(UserRole.Officer, user.Role);
        Assert.True(user.IsEmailVerified);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PartialUpdate_ShouldOnlyChangeProvided()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("test@test.com", "hash", "Original Name");
        _users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        await _sut.Handle(
            new UpdateUserCommand(userId, null, null, UserRole.Admin, null),
            CancellationToken.None);

        Assert.Equal("Original Name", user.FullName); // unchanged
        Assert.Equal(UserRole.Admin, user.Role);       // changed
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnError()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.Handle(
            new UpdateUserCommand(Guid.NewGuid(), "Name", null, null, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("USER_NOT_FOUND", result.Error.Code);
    }
}
