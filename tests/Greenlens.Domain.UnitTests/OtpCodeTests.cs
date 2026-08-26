using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.UnitTests;

public sealed class OtpCodeTests
{
    [Fact]
    public void HasExceededMaxAttempts_UsesConfiguredLimit_BR_AUTH_011()
    {
        var otp = OtpCode.Create("user@example.com", "hash", OtpPurpose.EmailVerification);

        otp.IncrementAttempt();
        otp.IncrementAttempt();
        otp.IncrementAttempt();

        Assert.True(otp.HasExceededMaxAttempts(3));
        Assert.False(otp.HasExceededMaxAttempts(5));
        Assert.True(otp.IsValid(5));
        Assert.False(otp.IsValid(3));
    }
}
