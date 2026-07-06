using System.Globalization;
using System.Text;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Reports.ExportReports;

/// <summary>
/// BR-OFF-022: Export reports as CSV or Excel.
/// Scope: LEO → own ward; DEO → own province; Admin → all.
/// PII excluded for non-Admin.
/// </summary>
public sealed class ExportReportsQueryHandler(
    IReportRepository reports,
    IUserRepository users,
    ICurrentUser currentUser)
    : IRequestHandler<ExportReportsQuery, Result<ExportReportsResponse>>
{
    public async Task<Result<ExportReportsResponse>> Handle(
        ExportReportsQuery request,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Result<ExportReportsResponse>.Failure(
                new Error("USER_NOT_FOUND", "Người dùng không tồn tại.", ErrorType.NotFound));

        // Build base query with scope filter
        var query = reports.QueryAsNoTracking();

        // Scope filter based on role
        var role = currentUser.Role;
        if (role == "LEO" && user.LocalOfficeId.HasValue)
            query = query.Where(r => r.AssignedOfficeId == user.LocalOfficeId);
        else if (role == "DEO" && user.DepartmentId.HasValue)
            query = query.Where(r => r.AssignedDepartmentId == user.DepartmentId);
        // Admin: no filter

        // Optional filters
        if (request.Status.HasValue)
            query = query.Where(r => r.Status == request.Status.Value);
        if (request.Severity.HasValue)
            query = query.Where(r => r.Severity == request.Severity.Value);
        if (request.From.HasValue)
            query = query.Where(r => r.CreatedAt >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(r => r.CreatedAt <= request.To.Value);

        // Project to export DTO
        var isAdmin = role == "Admin";
        var rows = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ExportRow(
                r.Code,
                r.Status.ToString(),
                r.Severity.ToString(),
                r.Latitude,
                r.Longitude,
                r.Address,
                r.Description,
                r.CreatedAt,
                r.SlaVerifyBreached,
                r.SlaResolveBreached,
                r.PriorityScore,
                // PII only for Admin
                isAdmin ? r.Reporter!.FullName : null,
                isAdmin ? r.Reporter!.PhoneNumber : null))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture);

        if (request.Format == ExportFormat.Excel)
        {
            var xlBytes = GenerateExcel(rows, isAdmin);
            return new ExportReportsResponse(
                xlBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"reports_{timestamp}.xlsx");
        }

        var csvBytes = GenerateCsv(rows, isAdmin);
        return new ExportReportsResponse(
            csvBytes,
            "text/csv",
            $"reports_{timestamp}.csv");
    }

    private static byte[] GenerateCsv(List<ExportRow> rows, bool includePii)
    {
        var sb = new StringBuilder();

        // Header
        sb.Append("Code,Status,Severity,Latitude,Longitude,Address,Description,CreatedAt,SlaVerifyBreached,SlaResolveBreached,PriorityScore");
        if (includePii)
            sb.Append(",ReporterName,ReporterPhone");
        sb.AppendLine();

        // Rows
        foreach (var r in rows)
        {
            sb.Append($"{Escape(r.Code)},{r.Status},{r.Severity},{r.Latitude},{r.Longitude}");
            sb.Append($",{Escape(r.Address)},{Escape(r.Description)}");
            sb.Append($",{r.CreatedAt:yyyy-MM-dd HH:mm},{r.SlaVerifyBreached},{r.SlaResolveBreached},{r.PriorityScore}");
            if (includePii)
                sb.Append($",{Escape(r.ReporterName)},{Escape(r.ReporterPhone)}");
            sb.AppendLine();
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static byte[] GenerateExcel(List<ExportRow> rows, bool includePii)
    {
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var ws = workbook.Worksheets.Add("Reports");

        // ── Header ──
        var headers = new List<string>
        {
            "Code", "Status", "Severity", "Latitude", "Longitude",
            "Address", "Description", "CreatedAt",
            "SlaVerifyBreached", "SlaResolveBreached", "PriorityScore"
        };
        if (includePii)
        {
            headers.Add("ReporterName");
            headers.Add("ReporterPhone");
        }

        for (var i = 0; i < headers.Count; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        // Style header row
        var headerRange = ws.Range(1, 1, 1, headers.Count);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#4472C4");
        headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

        // ── Data rows ──
        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            var rowNum = r + 2;

            ws.Cell(rowNum, 1).Value = row.Code;
            ws.Cell(rowNum, 2).Value = row.Status;
            ws.Cell(rowNum, 3).Value = row.Severity;
            ws.Cell(rowNum, 4).Value = (double)row.Latitude;
            ws.Cell(rowNum, 5).Value = (double)row.Longitude;
            ws.Cell(rowNum, 6).Value = row.Address ?? "";
            ws.Cell(rowNum, 7).Value = row.Description ?? "";
            ws.Cell(rowNum, 8).Value = row.CreatedAt;
            ws.Cell(rowNum, 8).Style.DateFormat.Format = "yyyy-MM-dd HH:mm";
            ws.Cell(rowNum, 9).Value = row.SlaVerifyBreached ? "Yes" : "No";
            ws.Cell(rowNum, 10).Value = row.SlaResolveBreached ? "Yes" : "No";
            ws.Cell(rowNum, 11).Value = (double)row.PriorityScore;

            if (includePii)
            {
                ws.Cell(rowNum, 12).Value = row.ReporterName ?? "";
                ws.Cell(rowNum, 13).Value = row.ReporterPhone ?? "";
            }
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private sealed record ExportRow(
        string Code,
        string Status,
        string Severity,
        decimal Latitude,
        decimal Longitude,
        string? Address,
        string? Description,
        DateTime CreatedAt,
        bool SlaVerifyBreached,
        bool SlaResolveBreached,
        decimal PriorityScore,
        string? ReporterName,
        string? ReporterPhone);
}
