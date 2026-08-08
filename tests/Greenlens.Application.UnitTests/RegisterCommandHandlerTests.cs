using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Auth.Register;
using Greenlens.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class RegisterCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IOtpRepository _otps = Substitute.For<IOtpRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IAuthEmailScheduler _authEmail = Substitute.For<IAuthEmailScheduler>();
    private readonly RegisterCommandHandler _sut;

    public RegisterCommandHandlerTests()
    {
        _sut = new RegisterCommandHandler(_users, _otps, _uow, _hasher, _authEmail, NullLogger<RegisterCommandHandler>.Instance);
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");
        _users.GetDeletedByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _authEmail.TryEnqueueOtpEmail(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
    }

    private static RegisterCommand CreateCommand(string email) =>
        new(email, "Password123!", "New User", true);

    [Fact]
    public async Task Handle_NewUser_ShouldSucceed()
    {
        _users.ExistsAsync(Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.Handle(CreateCommand("new@test.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("new@test.com", result.Value!.Email);
        _users.Received(1).Add(Arg.Any<User>());
        _otps.Received(1).Add(Arg.Any<OtpCode>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewUser_ShouldEnqueueOtpEmail()
    {
        _users.ExistsAsync(Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await _sut.Handle(CreateCommand("new@test.com"), CancellationToken.None);

        _authEmail.Received(1).TryEnqueueOtpEmail(
            "new@test.com",
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_EmailEnqueueFails_ReturnsEmailDispatchUnavailable()
    {
        _users.ExistsAsync(Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _authEmail.TryEnqueueOtpEmail(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await _sut.Handle(CreateCommand("new@test.com"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("EMAIL_DISPATCH_UNAVAILABLE", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ExistingEmail_ShouldReturnEmailTaken()
    {
        _users.ExistsAsync(Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(CreateCommand("exists@test.com"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("EMAIL_TAKEN", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_SoftDeletedEmail_ShouldReturnRestoreHint_BR_AUTH_021()
    {
        _users.ExistsAsync(Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _users.GetDeletedByEmailAsync("deleted@test.com", Arg.Any<CancellationToken>())
            .Returns(User.Create("deleted@test.com", "hash", "Deleted User"));

        var result = await _sut.Handle(CreateCommand("deleted@test.com"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("EMAIL_DELETED_RESTORE_AVAILABLE", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ExistingEmail_ShouldNotAddUser()
    {
        _users.ExistsAsync(Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _sut.Handle(CreateCommand("exists@test.com"), CancellationToken.None);

        _users.DidNotReceive().Add(Arg.Any<User>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
