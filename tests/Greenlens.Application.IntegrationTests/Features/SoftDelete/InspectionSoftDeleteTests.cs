using FluentAssertions;
using Greenlens.Application.Features.Inspection.DeleteViolatingEntity;
using Greenlens.Application.IntegrationTests.Fixtures;
using Greenlens.Application.IntegrationTests.Helpers;
using Greenlens.Domain.Common;

namespace Greenlens.Application.IntegrationTests.Features.SoftDelete;

[Collection("Postgres")]
public sealed class InspectionSoftDeleteTests(PostgresContainerFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task DeleteViolatingEntity_WhenInUse_ReturnsInUse_BR_INS_022()
    {
        var entityId = await WithDbAsync(async db =>
        {
            var entity = await IntegrationDataSeeder.SeedViolatingEntityAsync(db).ConfigureAwait(false);
            var category = await IntegrationDataSeeder.SeedCategoryAsync(db, $"CAT-{Guid.NewGuid():N}"[..8])
                .ConfigureAwait(false);
            await IntegrationDataSeeder.SeedInspectionForEntityAsync(db, entity, category)
                .ConfigureAwait(false);
            return entity.Id;
        }).ConfigureAwait(false);

        var result = await Mediator.Send(new DeleteViolatingEntityCommand(entityId)).ConfigureAwait(false);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("VIOLATING_ENTITY_IN_USE");
        result.Error.Type.Should().Be(ErrorType.BusinessRule);
    }

    [Fact]
    public async Task DeleteViolatingEntity_WhenAlreadyDeleted_ReturnsConflict_BR_INS_022()
    {
        var entityId = await WithDbAsync(async db =>
        {
            var entity = await IntegrationDataSeeder.SeedViolatingEntityAsync(db, softDeleted: true)
                .ConfigureAwait(false);
            return entity.Id;
        }).ConfigureAwait(false);

        var result = await Mediator.Send(new DeleteViolatingEntityCommand(entityId)).ConfigureAwait(false);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("VIOLATING_ENTITY_ALREADY_DELETED");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }
}
