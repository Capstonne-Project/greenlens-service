using FluentAssertions;
using Greenlens.Application.Features.Inspection.GetOfficerInspectionQueue;

namespace Greenlens.Application.UnitTests.Inspection;

public sealed class GetOfficerInspectionQueueQueryValidatorTests
{
    private readonly GetOfficerInspectionQueueQueryValidator _sut = new();

    [Fact]
    public void Validate_PageSizeOver100_ReturnsValidationError()
    {
        var result = _sut.Validate(new GetOfficerInspectionQueueQuery(PageSize: 101));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_DefaultQuery_IsValid()
    {
        var result = _sut.Validate(new GetOfficerInspectionQueueQuery());

        result.IsValid.Should().BeTrue();
    }
}
