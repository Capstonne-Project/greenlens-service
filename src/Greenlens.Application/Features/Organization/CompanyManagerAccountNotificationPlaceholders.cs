namespace Greenlens.Application.Features.Organization;

internal static class CompanyManagerAccountNotificationPlaceholders
{
    internal static Dictionary<string, string> ForCreated(
        string companyName,
        string managerFullName,
        string managerEmail,
        string tempPassword) =>
        new(StringComparer.Ordinal)
        {
            ["company_name"] = companyName,
            ["manager_name"] = managerFullName,
            ["email"] = managerEmail,
            ["temp_password"] = tempPassword
        };
}
