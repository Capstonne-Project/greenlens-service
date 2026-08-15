namespace Greenlens.Application.Common;

/// <summary>
/// Decides whether notification SMTP should be attempted for a mailbox.
/// </summary>
/// <remarks>
/// Seed / QA accounts (e.g. DEO/LEO <c>*@greenlens.dev</c>) have no real inbox — skip email
/// even when the user enabled email preference (BR-NTF-001). Push + in-app still deliver.
/// See <c>docs/SEED_ACCOUNTS.md</c>.
/// </remarks>
public static class NotificationEmailDeliverability
{
    private static readonly string[] NonDeliverableDomains =
    [
        "greenlens.dev", // LocalOfficeSeeder, MobileDemoSeeder
        "test.local",    // integration/unit test mailboxes
        "example.com",   // test mailboxes
    ];

    public static bool IsDeliverable(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var at = email.LastIndexOf('@');
        if (at < 0 || at >= email.Length - 1)
            return false;

        var domain = email[(at + 1)..];
        return !NonDeliverableDomains.Contains(domain, StringComparer.OrdinalIgnoreCase);
    }
}
