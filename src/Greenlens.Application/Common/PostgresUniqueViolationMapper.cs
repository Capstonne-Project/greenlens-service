using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

/// <summary>
/// Maps PostgreSQL unique-constraint violation messages to typed <see cref="Error"/> values.
/// Used as a safety net when pre-checks race with concurrent inserts.
/// </summary>
public static class PostgresUniqueViolationMapper
{
    public static bool IsUniqueViolation(Exception ex)
    {
        var message = GetMessage(ex);
        return message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
    }

    public static Error? TryMap(Exception ex)
    {
        if (!IsUniqueViolation(ex))
            return null;

        var message = GetMessage(ex);

        if (Contains(message, "ix_users_email", "users_email_key"))
            return Errors.Auth.EmailTaken;

        if (Contains(message, "phone_number", "ix_users_phone"))
            return Errors.Phone.PhoneAlreadyUsed;

        if (Contains(message, "google_id", "ix_users_google"))
            return Errors.Auth.EmailTaken;

        if (Contains(message, "ix_environmental_service_companies_contract_number", "contract_number"))
            return Errors.Organization.CompanyContractNumberExists;

        if (Contains(message, "ix_reports_code", "reports_code"))
            return Errors.Reports.ReportCodeConflict;

        if (Contains(message, "ix_team_members", "team_members_team_id_user_id"))
            return Errors.Organization.MemberAlreadyInTeam;

        if (Contains(message, "ix_pollution_categories_code", "pollution_categories_code"))
            return Errors.Reports.CategoryCodeExists;

        if (Contains(message, "ix_waste_tags_code", "waste_tags_code"))
            return Errors.Reports.WasteTagCodeExists;

        if (Contains(message, "ix_violating_entities_tax_code", "violating_entities_tax_code"))
            return Errors.Inspections.ViolatingEntityDuplicateTaxCode;

        if (Contains(message, "ix_violating_entities_identity_number", "violating_entities_identity_number"))
            return Errors.Inspections.ViolatingEntityDuplicateIdentityNumber;

        if (Contains(message, "ix_report_satisfactions_report_id_user_id", "report_satisfactions"))
            return Errors.Reports.AlreadyRated;

        return null;
    }

    private static string GetMessage(Exception ex) =>
        ex.InnerException?.Message ?? ex.Message;

    private static bool Contains(string message, params string[] hints) =>
        hints.Any(h => message.Contains(h, StringComparison.OrdinalIgnoreCase));
}
