using FluentAssertions;
using Greenlens.Application.Common;
using Greenlens.Application.Features.Auth.Register;
using Greenlens.Application.Features.Users.UpdateUser;
using Greenlens.Application.IntegrationTests.Fixtures;
using Greenlens.Application.IntegrationTests.Helpers;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.IntegrationTests.Features.Validation;

[Collection("Postgres")]
public sealed class AuthDuplicateTests(PostgresContainerFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Register_DuplicateActiveEmail_ReturnsEmailTaken_BR_AUTH_002()
    {
        const string email = "duplicate-active@test.local";
        await WithDbAsync(async db =>
        {
            var user = User.CreateByAdmin(email, "hash", "Existing User", UserRole.Citizen);
            db.Set<User>().Add(user);
            await db.SaveChangesAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);

        var result = await Mediator.Send(new RegisterCommand(
            email,
            "Password123!",
            "Duplicate User",
            true)).ConfigureAwait(false);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("EMAIL_TAKEN");
    }

    [Fact]
    public async Task Register_SoftDeletedEmail_ReturnsRestoreHint_BR_AUTH_021()
    {
        const string email = "deleted-restore@test.local";
        await WithDbAsync(async db =>
        {
            var user = User.CreateByAdmin(email, "hash", "Deleted User", UserRole.Citizen);
            user.SoftDelete("seed");
            db.Set<User>().Add(user);
            await db.SaveChangesAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);

        var result = await Mediator.Send(new RegisterCommand(
            email,
            "Password123!",
            "New User",
            true)).ConfigureAwait(false);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("EMAIL_DELETED_RESTORE_AVAILABLE");
    }

    [Fact]
    public async Task UpdateUser_DuplicatePhone_ReturnsPhoneAlreadyUsed()
    {
        var (targetUserId, otherPhone) = await WithDbAsync(async db =>
        {
            var other = User.CreateByAdmin($"other-{Guid.NewGuid():N}@test.local", "hash", "Other", UserRole.Citizen);
            other.VerifyPhone("84901234567");
            db.Set<User>().Add(other);

            var target = User.CreateByAdmin($"target-{Guid.NewGuid():N}@test.local", "hash", "Target", UserRole.Admin);
            db.Set<User>().Add(target);
            await db.SaveChangesAsync().ConfigureAwait(false);
            return (target.Id, other.PhoneNumber!);
        }).ConfigureAwait(false);

        var result = await Mediator.Send(new UpdateUserCommand(
            targetUserId,
            null,
            otherPhone,
            null,
            null)).ConfigureAwait(false);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("PHONE_ALREADY_USED");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }
}
