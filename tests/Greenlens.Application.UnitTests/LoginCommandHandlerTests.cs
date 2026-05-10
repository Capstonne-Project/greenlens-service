using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Auth.Login;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class LoginCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IJwtService _jwt = Substitute.For<IJwtService>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly LoginCommandHandler _sut;

    public LoginCommandHandlerTests()
    {
        _sut = new LoginCommandHandler(_users, _refreshTokens, _uow, _jwt, _hasher);
    }

    private static User CreateUser(string email = "test@test.com") =>
        User.Create(email, "hash", "Test User");

    [Fact]
    public async Task Handle_ValidCredentials_ShouldReturnTokens()
    {
        var user = CreateUser();
        _users.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("password123", Arg.Any<string>()).Returns(true);
        _jwt.GenerateAccessToken(Arg.Any<User>()).Returns("access-token");
        _jwt.GenerateRefreshToken().Returns("refresh-token");
        _jwt.HashToken("refresh-token").Returns("hashed-refresh");

        var result = await _sut.Handle(new LoginCommand("test@test.com", "password123"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value.AccessToken);
        Assert.Equal("refresh-token", result.Value.RefreshToken);
        Assert.Equal(user.Email, result.Value.User.Email);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnInvalidCredentials()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.Handle(new LoginCommand("unknown@test.com", "pass"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_CREDENTIALS", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WrongPassword_ShouldRecordFailedLogin()
    {
        var user = CreateUser();
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await _sut.Handle(new LoginCommand("test@test.com", "wrong"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_CREDENTIALS", result.Error.Code);
        Assert.Equal(1, user.FailedLoginAttempts);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LockedOutUser_ShouldReturnAccountLocked()
    {
        var user = CreateUser();
        for (int i = 0; i < 5; i++) user.RecordFailedLogin();

        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(new LoginCommand("test@test.com", "pass"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("ACCOUNT_LOCKED", result.Error.Code);
    }

    [Fact]
    public async Task Handle_SuccessfulLogin_ShouldResetFailedAttempts()
    {
        var user = CreateUser();
        user.RecordFailedLogin();
        user.RecordFailedLogin();

        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _jwt.GenerateAccessToken(Arg.Any<User>()).Returns("token");
        _jwt.GenerateRefreshToken().Returns("refresh");
        _jwt.HashToken(Arg.Any<string>()).Returns("hashed");

        await _sut.Handle(new LoginCommand("test@test.com", "correct"), CancellationToken.None);

        Assert.Equal(0, user.FailedLoginAttempts);
    }

    [Fact]
    public async Task Handle_SuccessfulLogin_ShouldSaveRefreshToken()
    {
        var user = CreateUser();
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _jwt.GenerateAccessToken(Arg.Any<User>()).Returns("at");
        _jwt.GenerateRefreshToken().Returns("rt");
        _jwt.HashToken("rt").Returns("hashed-rt");

        await _sut.Handle(new LoginCommand("test@test.com", "pass"), CancellationToken.None);

        _refreshTokens.Received(1).Add(Arg.Any<RefreshToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailCaseInsensitive_ShouldNormalize()
    {
        _users.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(CreateUser());
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _jwt.GenerateAccessToken(Arg.Any<User>()).Returns("t");
        _jwt.GenerateRefreshToken().Returns("r");
        _jwt.HashToken(Arg.Any<string>()).Returns("h");

        await _sut.Handle(new LoginCommand("TEST@TEST.COM", "pass"), CancellationToken.None);

        await _users.Received(1).GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>());
    }
}
