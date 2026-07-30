using FluentAssertions;
using Greenlens.Application.Common;
using Greenlens.Application.Features.Admin.AuditLogs.ExportAuditLogs;
using Greenlens.Application.Features.Admin.AuditLogs.GetAuditLogById;
using Greenlens.Application.Features.Admin.AuditLogs.GetAuditLogs;
using Greenlens.Application.IntegrationTests.Fixtures;
using Greenlens.Application.IntegrationTests.Helpers;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.IntegrationTests.Features.Admin;

[Collection("Postgres")]
public sealed class AuditLogQueryTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetAuditLogById_NotFound_Returns404_BR_ADM_010()
    {
        var result = await Mediator.Send(new GetAuditLogByIdQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(Errors.Admin.AuditLogNotFound.Code);
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetAuditLogs_FilterByEntityId_ReturnsMatching_BR_ADM_010()
    {
        var targetEntityId = Guid.NewGuid().ToString();

        await WithDbAsync(async db =>
        {
            var actor = await IntegrationDataSeeder.SeedUserAsync(db);

            db.Set<AuditLog>().Add(AuditLog.Create(
                actor.Id,
                "UpdateUserRole",
                "User",
                targetEntityId,
                oldValues: null,
                newValues: "{\"newRole\":\"LEO\"}",
                ipAddress: "127.0.0.1",
                userAgent: "test-agent"));

            db.Set<AuditLog>().Add(AuditLog.Create(
                actor.Id,
                "UpdateUserRole",
                "User",
                Guid.NewGuid().ToString(),
                oldValues: null,
                newValues: "{\"newRole\":\"Citizen\"}",
                ipAddress: "127.0.0.1",
                userAgent: "test-agent"));

            await db.SaveChangesAsync();
        });

        var result = await Mediator
            .Send(new GetAuditLogsQuery(EntityId: targetEntityId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].EntityId.Should().Be(targetEntityId);
    }

    [Fact]
    public async Task GetAuditLogs_FilterByEntityTypeReport_ReturnsOfficerActions_BR_ADM_010()
    {
        var reportId = Guid.NewGuid().ToString();

        await WithDbAsync(async db =>
        {
            var actor = await IntegrationDataSeeder.SeedUserAsync(db);

            db.Set<AuditLog>().Add(AuditLog.Create(
                actor.Id,
                "VerifyReport",
                "Report",
                reportId,
                oldValues: "{\"status\":\"Submitted\"}",
                newValues: "{\"status\":\"Verified\"}",
                ipAddress: "127.0.0.1",
                userAgent: "test-agent"));

            db.Set<AuditLog>().Add(AuditLog.Create(
                actor.Id,
                "CreateCategory",
                "PollutionCategory",
                Guid.NewGuid().ToString(),
                oldValues: null,
                newValues: "{}",
                ipAddress: "127.0.0.1",
                userAgent: "test-agent"));

            await db.SaveChangesAsync();
        });

        var result = await Mediator
            .Send(new GetAuditLogsQuery(EntityType: "Report"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(i => i.Action == "VerifyReport");
    }

    [Fact]
    public async Task GetAuditLogs_ReturnsActorRole_BR_ADM_010()
    {
        await WithDbAsync(async db =>
        {
            var actor = await IntegrationDataSeeder.SeedUserAsync(db, UserRole.LEO);

            db.Set<AuditLog>().Add(AuditLog.Create(
                actor.Id,
                "VerifyReport",
                "Report",
                Guid.NewGuid().ToString(),
                oldValues: null,
                newValues: "{}",
                ipAddress: "127.0.0.1",
                userAgent: "test-agent"));

            await db.SaveChangesAsync();
        });

        var result = await Mediator
            .Send(new GetAuditLogsQuery(EntityType: "Report"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(i => i.ActorRole == UserRole.LEO);
    }

    [Fact]
    public async Task GetAuditLogs_FilterByActorRole_ReturnsMatching_BR_ADM_010()
    {
        await WithDbAsync(async db =>
        {
            var leo = await IntegrationDataSeeder.SeedUserAsync(db, UserRole.LEO);
            var admin = await IntegrationDataSeeder.SeedUserAsync(db, UserRole.Admin);

            db.Set<AuditLog>().Add(AuditLog.Create(
                leo.Id,
                "VerifyReport",
                "Report",
                Guid.NewGuid().ToString(),
                oldValues: null,
                newValues: "{}",
                ipAddress: "127.0.0.1",
                userAgent: "test-agent"));

            db.Set<AuditLog>().Add(AuditLog.Create(
                admin.Id,
                "ToggleBanUser",
                "User",
                Guid.NewGuid().ToString(),
                oldValues: null,
                newValues: "{}",
                ipAddress: "127.0.0.1",
                userAgent: "test-agent"));

            await db.SaveChangesAsync();
        });

        var result = await Mediator
            .Send(new GetAuditLogsQuery(ActorRole: UserRole.LEO));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().OnlyContain(i => i.ActorRole == UserRole.LEO);
        result.Value.Items.Should().Contain(i => i.Action == "VerifyReport");
    }

    [Fact]
    public async Task ExportAuditLogs_StreamsRows_BR_ADM_010()
    {
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        await WithDbAsync(async db =>
        {
            var actor = await IntegrationDataSeeder.SeedUserAsync(db);

            db.Set<AuditLog>().Add(AuditLog.Create(
                actor.Id,
                "ToggleBanUser",
                "User",
                Guid.NewGuid().ToString(),
                oldValues: null,
                newValues: "{}",
                ipAddress: "127.0.0.1",
                userAgent: "test-agent"));

            await db.SaveChangesAsync();
        });

        var result = await Mediator
            .Send(new ExportAuditLogsQuery(from, to));

        result.IsSuccess.Should().BeTrue();
        result.Value!.ContentType.Should().Be("text/csv");
        result.Value.Content.Should().NotBeEmpty();
    }
}
