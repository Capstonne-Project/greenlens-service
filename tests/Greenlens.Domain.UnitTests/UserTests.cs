using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Domain.Exceptions;

namespace Greenlens.Domain.UnitTests;

public sealed class UserTests
{
    [Fact]
    public void Create_ShouldSetDefaults()
    {
        var user = User.Create("Test@Email.COM", "hash123", "Nguyễn Văn A");

        Assert.Equal("test@email.com", user.Email); // lowercase
        Assert.Equal("hash123", user.PasswordHash);
        Assert.Equal("Nguyễn Văn A", user.FullName);
        Assert.Equal(UserRole.Citizen, user.Role);
        Assert.False(user.IsEmailVerified);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LockoutEnd);
    }

    [Fact]
    public void Create_WithAdminRole_ShouldSetRole()
    {
        var user = User.Create("admin@test.com", "hash", "Admin", UserRole.Admin);

        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public void CreateFromGoogle_ShouldAutoVerifyEmail()
    {
        var user = User.CreateFromGoogle("user@gmail.com", "Google User", "google-id-123", "https://avatar.url");

        Assert.Equal("user@gmail.com", user.Email);
        Assert.True(user.IsEmailVerified);
        Assert.Equal("google-id-123", user.GoogleId);
        Assert.Equal("https://avatar.url", user.AvatarUrl);
        Assert.Equal(string.Empty, user.PasswordHash);
    }

    [Fact]
    public void RecordFailedLogin_Under5_ShouldNotLockout()
    {
        var user = User.Create("test@test.com", "hash", "Test");

        user.RecordFailedLogin();
        user.RecordFailedLogin();
        user.RecordFailedLogin();

        Assert.Equal(3, user.FailedLoginAttempts);
        Assert.False(user.IsLockedOut());
        Assert.True(user.RequiresCaptcha()); // ≥3 → captcha
    }

    [Fact]
    public void RecordFailedLogin_5Times_ShouldLockout()
    {
        var user = User.Create("test@test.com", "hash", "Test");

        for (int i = 0; i < 5; i++)
            user.RecordFailedLogin();

        Assert.Equal(5, user.FailedLoginAttempts);
        Assert.True(user.IsLockedOut());
        Assert.NotNull(user.LockoutEnd);
    }

    [Fact]
    public void ResetFailedLoginAttempts_ShouldClearLockout()
    {
        var user = User.Create("test@test.com", "hash", "Test");
        for (int i = 0; i < 5; i++)
            user.RecordFailedLogin();

        user.ResetFailedLoginAttempts();

        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LockoutEnd);
        Assert.False(user.IsLockedOut());
    }

    [Fact]
    public void VerifyEmail_WhenNotVerified_ShouldSucceed()
    {
        var user = User.Create("test@test.com", "hash", "Test");

        user.VerifyEmail();

        Assert.True(user.IsEmailVerified);
    }

    [Fact]
    public void VerifyEmail_WhenAlreadyVerified_ShouldThrow()
    {
        var user = User.Create("test@test.com", "hash", "Test");
        user.VerifyEmail();

        Assert.Throws<DomainException>(() => user.VerifyEmail());
    }

    [Fact]
    public void UpdateProfile_ShouldOnlyUpdateProvidedFields()
    {
        var user = User.Create("test@test.com", "hash", "Old Name");

        user.UpdateProfile(fullName: "New Name");

        Assert.Equal("New Name", user.FullName);
        Assert.Null(user.PhoneNumber); // unchanged
        Assert.Null(user.AvatarUrl);   // unchanged
    }

    [Fact]
    public void UpdateProfile_AllFields_ShouldUpdateAll()
    {
        var user = User.Create("test@test.com", "hash", "Name");

        user.UpdateProfile("New Name", "https://avatar.url");

        Assert.Equal("New Name", user.FullName);
        Assert.Equal("https://avatar.url", user.AvatarUrl);
    }

    [Fact]
    public void VerifyPhone_ShouldSetPhoneAndMarkVerified()
    {
        var user = User.Create("test@test.com", "hash", "Test");

        user.VerifyPhone("84901234567");

        Assert.Equal("84901234567", user.PhoneNumber);
        Assert.True(user.IsPhoneVerified);
    }

    [Fact]
    public void AdminUpdate_ShouldChangeRoleAndVerification()
    {
        var user = User.Create("test@test.com", "hash", "Test");

        user.AdminUpdate(role: UserRole.LEO, isEmailVerified: true);

        Assert.Equal(UserRole.LEO, user.Role);
        Assert.True(user.IsEmailVerified);
    }

    [Fact]
    public void SoftDelete_ShouldSetDeletedAt()
    {
        var user = User.Create("test@test.com", "hash", "Test");

        user.SoftDelete("admin-user-id");

        Assert.True(user.IsDeleted);
        Assert.NotNull(user.DeletedAt);
        Assert.Equal("admin-user-id", user.DeletedBy);
    }

    [Fact]
    public void Restore_AfterSoftDelete_ShouldClearDeletedAt()
    {
        var user = User.Create("test@test.com", "hash", "Test");
        user.SoftDelete();

        user.Restore();

        Assert.False(user.IsDeleted);
        Assert.Null(user.DeletedAt);
    }

    [Fact]
    public void ChangePassword_ShouldUpdateHash()
    {
        var user = User.Create("test@test.com", "old-hash", "Test");

        user.ChangePassword("new-hash");

        Assert.Equal("new-hash", user.PasswordHash);
    }

    [Fact]
    public void LinkGoogleAccount_ShouldSetGoogleId()
    {
        var user = User.Create("test@test.com", "hash", "Test");

        user.LinkGoogleAccount("google-id-456");

        Assert.Equal("google-id-456", user.GoogleId);
    }

    [Fact]
    public void RequiresCaptcha_Under3Attempts_ShouldBeFalse()
    {
        var user = User.Create("test@test.com", "hash", "Test");
        user.RecordFailedLogin();
        user.RecordFailedLogin();

        Assert.False(user.RequiresCaptcha());
    }
}
