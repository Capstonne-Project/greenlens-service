namespace Greenlens.Infrastructure.Seeders;

/// <summary>Loads repo-root .env into process environment for CLI seed/upload tools.</summary>
internal static class RepoDotEnvLoader
{
    internal static void LoadIfPresent()
    {
        foreach (var path in ResolveCandidates())
        {
            if (!File.Exists(path))
                continue;

            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                var eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;

                var key = line[..eq].Trim();
                var value = line[(eq + 1)..].Trim().Trim('"');
                Environment.SetEnvironmentVariable(key, value);
            }

            return;
        }
    }

    private static IEnumerable<string> ResolveCandidates()
    {
        yield return Path.Combine(Directory.GetCurrentDirectory(), ".env");
        yield return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".env"));
        yield return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".env"));
    }
}
