namespace Greenlens.Application.Features.Organization;

internal static class CompanyStaffAccountNotificationPlaceholders
{
    internal static Dictionary<string, string> ForCreated(
        string companyName,
        string staffFullName,
        string staffEmail,
        string tempPassword) =>
        new(StringComparer.Ordinal)
        {
            ["company_name"] = companyName,
            ["staff_name"] = staffFullName,
            ["email"] = staffEmail,
            ["temp_password"] = tempPassword
        };
}
