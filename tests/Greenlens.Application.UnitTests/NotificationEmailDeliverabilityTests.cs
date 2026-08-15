using FluentAssertions;
using Greenlens.Application.Common;

namespace Greenlens.Application.UnitTests;

public sealed class NotificationEmailDeliverabilityTests
{
    [Theory]
    [InlineData("deo.79@greenlens.dev")]
    [InlineData("leo.00004@greenlens.dev")]
    [InlineData("company@greenlens.dev")]
    [InlineData("user-abc@test.local")]
    public void IsDeliverable_SeedOrTestMailbox_ReturnsFalse(string email)
    {
        NotificationEmailDeliverability.IsDeliverable(email).Should().BeFalse();
    }

    [Theory]
    [InlineData("hieutran4525@gmail.com")]
    [InlineData("manager@dvmt-company.vn")]
    [InlineData("noreply@greenlens.online")]
    public void IsDeliverable_RealMailbox_ReturnsTrue(string email)
    {
        NotificationEmailDeliverability.IsDeliverable(email).Should().BeTrue();
    }

    [Fact]
    public void IsDeliverable_NullOrBlank_ReturnsFalse()
    {
        NotificationEmailDeliverability.IsDeliverable(null).Should().BeFalse();
        NotificationEmailDeliverability.IsDeliverable("   ").Should().BeFalse();
    }
}
