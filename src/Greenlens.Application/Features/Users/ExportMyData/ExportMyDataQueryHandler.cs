using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Users.ExportMyData;

/// <summary>
/// Gathers all personal data for the authenticated user and returns as JSON or CSV.
/// Covers: profile, reports, notifications, gamification (points + badges).
/// </summary>
/// <remarks>Implements: BR-DAT-003 (right to access / download personal data).</remarks>
public sealed class ExportMyDataQueryHandler(
    IUserRepository users,
    IReportRepository reports,
    INotificationRepository notifications,
    IUserPointsRepository userPoints,
    IUserBadgeRepository userBadges,
    ICurrentUser currentUser,
    ILogger<ExportMyDataQueryHandler> logger)
    : IRequestHandler<ExportMyDataQuery, Result<ExportMyDataResponse>>
{
    public async Task<Result<ExportMyDataResponse>> Handle(
        ExportMyDataQuery request,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("User not found for ID {UserId}", currentUser.UserId);
            return Errors.Auth.UserNotFound;
        }

        // ── Gather personal data ─────────────────────────────────────────────
        var myReports = await reports.QueryAsNoTracking()
            .Where(r => r.ReporterId == currentUser.UserId)
            .Select(r => new ExportReport(
                r.Code, r.Description, r.Status.ToString(), r.Severity.ToString(),
                r.Latitude, r.Longitude, r.Address, r.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var myNotifications = await notifications.QueryAsNoTracking()
            .Where(n => n.RecipientId == currentUser.UserId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(500) // cap to avoid huge exports
            .Select(n => new ExportNotification(
                n.Title, n.Message, n.Type.ToString(), n.IsRead, n.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var points = await userPoints.QueryAsNoTracking()
            .Where(p => p.UserId == currentUser.UserId)
            .Select(p => new ExportPoints(p.TotalPoints))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var badges = await userBadges.QueryAsNoTracking()
            .Where(b => b.UserId == currentUser.UserId)
            .Select(b => new ExportBadge(b.Badge!.NameVi, b.AwardedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var exportData = new PersonalDataExport(
            Profile: new ExportProfile(
                user.Email, user.FullName, user.PhoneNumber, user.AvatarUrl,
                user.Role.ToString(), user.IsEmailVerified, user.IsPhoneVerified,
                user.HasDataConsent, user.ConsentAcceptedAt,
                user.CreatedAt),
            Reports: myReports,
            Notifications: myNotifications,
            Points: points,
            Badges: badges,
            ExportedAt: DateTime.UtcNow);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        if (request.Format == ExportMyDataFormat.Csv)
        {
            var csvBytes = GenerateCsv(exportData);
            logger.LogInformation("User {UserId} exported personal data as CSV", user.Id);
            return new ExportMyDataResponse(csvBytes, "text/csv", $"my_data_{timestamp}.csv");
        }

        var jsonBytes = GenerateJson(exportData);
        logger.LogInformation("User {UserId} exported personal data as JSON", user.Id);
        return new ExportMyDataResponse(jsonBytes,
            "application/json",
            $"my_data_{timestamp}.json");
    }

    private static byte[] GenerateJson(PersonalDataExport data)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
        return JsonSerializer.SerializeToUtf8Bytes(data, options);
    }

    private static byte[] GenerateCsv(PersonalDataExport data)
    {
        var sb = new StringBuilder();

        // ── Profile section ──
        sb.AppendLine("=== PROFILE ===");
        sb.AppendLine("Email,FullName,Phone,Role,EmailVerified,PhoneVerified,DataConsent,ConsentAt,CreatedAt");
        var p = data.Profile;
        sb.AppendLine($"{Esc(p.Email)},{Esc(p.FullName)},{Esc(p.Phone)},{p.Role},{p.EmailVerified},{p.PhoneVerified},{p.DataConsent},{p.ConsentAcceptedAt:yyyy-MM-dd HH:mm},{p.CreatedAt:yyyy-MM-dd HH:mm}");

        // ── Reports section ──
        sb.AppendLine();
        sb.AppendLine("=== REPORTS ===");
        sb.AppendLine("Code,Description,Status,Severity,Latitude,Longitude,Address,CreatedAt");
        foreach (var r in data.Reports)
        {
            sb.AppendLine($"{Esc(r.Code)},{Esc(r.Description)},{r.Status},{r.Severity},{r.Latitude},{r.Longitude},{Esc(r.Address)},{r.CreatedAt:yyyy-MM-dd HH:mm}");
        }

        // ── Notifications section ──
        sb.AppendLine();
        sb.AppendLine("=== NOTIFICATIONS ===");
        sb.AppendLine("Title,Body,Type,IsRead,CreatedAt");
        foreach (var n in data.Notifications)
        {
            sb.AppendLine($"{Esc(n.Title)},{Esc(n.Body)},{n.Type},{n.IsRead},{n.CreatedAt:yyyy-MM-dd HH:mm}");
        }

        // ── Gamification section ──
        sb.AppendLine();
        sb.AppendLine("=== GAMIFICATION ===");
        if (data.Points is not null)
            sb.AppendLine($"TotalPoints: {data.Points.TotalPoints}");
        sb.AppendLine("Badges:");
        foreach (var b in data.Badges)
        {
            sb.AppendLine($"  {Esc(b.Name)} (earned {b.AwardedAt:yyyy-MM-dd})");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string Esc(string? v)
    {
        if (string.IsNullOrEmpty(v)) return "";
        if (v.Contains(',') || v.Contains('"') || v.Contains('\n'))
            return $"\"{v.Replace("\"", "\"\"")}\"";
        return v;
    }
}

// ── Export DTOs (internal, not exposed) ──
internal sealed record PersonalDataExport(
    ExportProfile Profile,
    List<ExportReport> Reports,
    List<ExportNotification> Notifications,
    ExportPoints? Points,
    List<ExportBadge> Badges,
    DateTime ExportedAt);

internal sealed record ExportProfile(
    string Email, string FullName, string? Phone, string? AvatarUrl,
    string Role, bool EmailVerified, bool PhoneVerified,
    bool DataConsent, DateTime? ConsentAcceptedAt,
    DateTime CreatedAt);

internal sealed record ExportReport(
    string Code, string? Description, string Status, string Severity,
    decimal Latitude, decimal Longitude, string? Address, DateTime CreatedAt);

internal sealed record ExportNotification(
    string Title, string Body, string Type, bool IsRead, DateTime CreatedAt);

internal sealed record ExportPoints(int TotalPoints);

internal sealed record ExportBadge(string Name, DateTime AwardedAt);
