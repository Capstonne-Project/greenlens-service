namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Sends SMS messages via an external provider (SpeedSMS, Twilio, etc.).
/// </summary>
public interface ISmsSender
{
    /// <summary>
    /// Send an OTP code via SMS to the given phone number.
    /// </summary>
    /// <returns>True if the SMS was sent successfully.</returns>
    Task<bool> SendOtpAsync(string phoneNumber, string otpCode, CancellationToken ct = default);
}
