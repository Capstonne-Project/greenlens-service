namespace Greenlens.Api.Configuration;

/// <summary>
/// Loads key/value pairs from a <c>.env</c> file into process environment variables
/// before <see cref="WebApplication.CreateBuilder"/> runs.
/// </summary>
/// <remarks>
/// ASP.NET Core maps env vars to configuration via double-underscore nesting
/// (e.g. <c>Jwt__Secret</c> → <c>Jwt:Secret</c>).
/// Existing environment variables are not overwritten.
/// </remarks>
internal static class DotEnvLoader
{
    public static void Load()
    {
        var envPath = FindEnvFile();
        if (envPath is null)
            return;

        foreach (var line in File.ReadAllLines(envPath))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            var key = trimmed[..separatorIndex].Trim();
            if (string.IsNullOrEmpty(key))
                continue;

            var value = trimmed[(separatorIndex + 1)..].Trim();
            value = Unquote(value);

            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string? FindEnvFile()
    {
        foreach (var startDir in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = startDir;
            while (!string.IsNullOrEmpty(dir))
            {
                var candidate = Path.Combine(dir, ".env");
                if (File.Exists(candidate))
                    return candidate;

                var parent = Directory.GetParent(dir)?.FullName;
                if (parent is null || parent == dir)
                    break;

                dir = parent;
            }
        }

        return null;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 &&
            ((value.StartsWith('"') && value.EndsWith('"')) ||
             (value.StartsWith('\'') && value.EndsWith('\''))))
        {
            return value[1..^1];
        }

        return value;
    }
}
