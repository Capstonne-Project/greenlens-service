using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Users.UploadUserAvatar;
using Greenlens.Domain.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Greenlens.Application.UnitTests;

public sealed class UploadUserAvatarCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IFileStorageService _fileStorage = Substitute.For<IFileStorageService>();
    private readonly ILogger<UploadUserAvatarCommandHandler> _logger =
        Substitute.For<ILogger<UploadUserAvatarCommandHandler>>();
    private readonly UploadUserAvatarCommandHandler _sut;

    private static readonly Guid UserId = Guid.NewGuid();

    public UploadUserAvatarCommandHandlerTests()
    {
        _currentUser.UserId.Returns(UserId);
        _sut = new UploadUserAvatarCommandHandler(_users, _uow, _currentUser, _fileStorage, _logger);
    }

    private static UploadUserAvatarCommand CreateCommand(
        string contentType = "image/jpeg", long size = 1_000_000) =>
        new(Stream.Null, "avatar.jpg", contentType, size);

    [Fact]
    public async Task Handle_ValidAvatar_ShouldUploadAndUpdateUser()
    {
        var user = User.Create("test@test.com", "hash", "Test");
        _users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _fileStorage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new FileUploadResult("https://cdn/avatar.jpg", "avatars/abc_avatar.jpg"));

        var result = await _sut.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://cdn/avatar.jpg", result.Value.AvatarUrl);
        Assert.Equal("https://cdn/avatar.jpg", user.AvatarUrl);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("video/mp4")]
    [InlineData("application/pdf")]
    public async Task Handle_InvalidContentType_ShouldReturnError(string contentType)
    {
        var result = await _sut.Handle(CreateCommand(contentType: contentType), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_FILE_TYPE", result.Error.Code);
    }

    [Fact]
    public async Task Handle_FileTooLarge_ShouldReturnError()
    {
        var result = await _sut.Handle(CreateCommand(size: 6 * 1024 * 1024), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("FILE_TOO_LARGE", result.Error.Code);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnError()
    {
        _users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.Handle(CreateCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("USER_NOT_FOUND", result.Error.Code);
    }

    [Fact]
    public async Task Handle_StorageThrows_ShouldReturnStorageError()
    {
        var user = User.Create("test@test.com", "hash", "Test");
        _users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _fileStorage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("R2 down"));

        var result = await _sut.Handle(CreateCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("STORAGE_UPLOAD_FAILED", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ShouldUploadToAvatarsFolder()
    {
        var user = User.Create("test@test.com", "hash", "Test");
        _users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _fileStorage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new FileUploadResult("url", "key"));

        await _sut.Handle(CreateCommand(), CancellationToken.None);

        await _fileStorage.Received(1).UploadAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), "image/jpeg", "avatars", Arg.Any<CancellationToken>());
    }
}
