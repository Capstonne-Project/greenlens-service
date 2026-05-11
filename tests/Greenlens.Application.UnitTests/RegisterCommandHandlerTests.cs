using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Auth.Register;
using Greenlens.Domain.Entities;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class RegisterCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IOtpRepository _otps = Substitute.For<IOtpRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IEmailSender _email = Substitute.For<IEmailSender>();
    private readonly RegisterCommandHandler _sut;

    public RegisterCommandHandlerTests()
    {
        _sut = new RegisterCommandHandler(_users, _otps, _uow, _hasher, _email);
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");
    }

    [Fact]
    public async Task Handle_NewUser_ShouldSucceed()
    {
        _users.ExistsAsync(Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.Handle(
            new RegisterCommand("new@test.com", "Password123!", "New User"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("new@test.com", result.Value.Email);
        _users.Received(1).Add(Arg.Any<User>());
        _otps.Received(1).Add(Arg.Any<OtpCode>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewUser_ShouldSendOtpEmail()
    {
        _users.ExistsAsync(Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await _sut.Handle(
            new RegisterCommand("new@test.com", "Pass123!", "Test"),
            CancellationToken.None);

        await _email.Received(1).SendOtpAsync(
            "new@test.com",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingEmail_ShouldReturnEmailTaken()
    {
        _users.ExistsAsync(Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(
            new RegisterCommand("exists@test.com", "Pass123!", "User"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("EMAIL_TAKEN", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ExistingEmail_ShouldNotAddUser()
    {
        _users.ExistsAsync(Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _sut.Handle(
            new RegisterCommand("exists@test.com", "Pass123!", "User"),
            CancellationToken.None);

        _users.DidNotReceive().Add(Arg.Any<User>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
