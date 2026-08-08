using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Application.IntegrationTests.Fixtures;

/// <summary>No-op email scheduler for integration tests — avoids Hangfire dependency.</summary>
internal sealed class NoOpAuthEmailScheduler : IAuthEmailScheduler
{
    public bool TryEnqueueOtpEmail(string toEmail, string otpCode, string purpose) => true;
    public bool TryEnqueuePasswordResetEmail(string toEmail, string otpCode) => true;
}
