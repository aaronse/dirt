using System.Text.RegularExpressions;

namespace DirT;

internal enum DirectiveKind { Exclude, Include }

internal sealed class PatternSet
{
    public IReadOnlyList<string> Patterns => _patterns;
    private readonly List<string> _patterns;

    private PatternSet(IEnumerable<string> patterns) => _patterns = patterns.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();

    public static PatternSet DefaultExcludes() => new(new[]
    {
        "bin", "obj", "node_modules", ".git", ".vs", ".idea",
        "dist", "build", "out", "target", "coverage", ".cache",
        "packages"
    });

    // Empty includes means "include everything"
    public static PatternSet EmptyIncludes() => new(Array.Empty<string>());

    public PatternSet ApplyDirective(string directive, DirectiveKind kind)
    {
        // directive may start with + or - to add/remove; otherwise replace.
        var trimmed = directive.Trim();
        if (trimmed.Length == 0) return this;

        var mode = 'r';
        if (trimmed[0] is '+' or '-')
        {
            mode = trimmed[0];
            trimmed = trimmed[1..];
        }

        var items = SplitExprList(trimmed);

        if (mode == 'r')
            return new PatternSet(items);

        if (mode == '+')
        {
            var merged = _patterns.Concat(items).Distinct(StringComparer.OrdinalIgnoreCase);
            return new PatternSet(merged);
        }

        // mode == '-'
        {
            var remove = new HashSet<string>(items, StringComparer.OrdinalIgnoreCase);
            var kept = _patterns.Where(p => !remove.Contains(p));
            return new PatternSet(kept);
        }
    }

    private static IEnumerable<string> SplitExprList(string exprs)
        => exprs.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public bool HasAny() => _patterns.Count > 0;

    public bool MatchesPath(string relativePath, bool isDirectory)
    {
        // relativePath uses '/' separators.
        foreach (var pat in _patterns)
        {
            if (GlobMatcher.IsMatch(relativePath, pat, isDirectory)) return true;
        }
        return false;
    }

    public bool MatchesName(string name, bool isDirectory)
    {
        foreach (var pat in _patterns)
        {
            if (GlobMatcher.IsMatch(name, pat, isDirectory)) return true;

            // Convenience: if pattern has no glob chars and no slashes, treat as "match any segment name"
            if (!GlobMatcher.HasGlob(pat) && !pat.Contains('/') && !pat.Contains('\\'))
            {
                if (name.Equals(pat, StringComparison.OrdinalIgnoreCase)) return true;
            }
        }
        return false;
    }

    public string? FirstMatch(string relativePath, string name, bool isDirectory)
    {
        foreach (var pat in _patterns)
        {
            if (GlobMatcher.IsMatch(relativePath, pat, isDirectory)) return pat;
            if (GlobMatcher.IsMatch(name, pat, isDirectory)) return pat;

            if (!GlobMatcher.HasGlob(pat) && !pat.Contains('/') && !pat.Contains('\\'))
            {
                if (name.Equals(pat, StringComparison.OrdinalIgnoreCase)) return pat;
            }
        }
        return null;
    }
}

internal static class GlobMatcher
{
    public static bool HasGlob(string pattern) => pattern.IndexOfAny(new[] { '*', '?', '[', ']' }) >= 0;

    public static bool IsMatch(string pathOrName, string pattern, bool isDirectory)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return false;

        // Normalize pattern: treat backslashes as slashes.
        pattern = pattern.Replace('\\', '/').Trim();

        // Directory-only patterns: trailing '/'
        var dirOnly = pattern.EndsWith('/');
        if (dirOnly)
        {
            if (!isDirectory) return false;
            pattern = pattern.TrimEnd('/');
        }

        // If pattern contains slashes, match against full relative path; else match against last segment too.
        var normalized = pathOrName.Replace('\\', '/');

        // If pattern is a bare name, allow segment match (caller may already do this; keep it here too).
        if (!pattern.Contains('/') && !HasGlob(pattern))
        {
            var last = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? normalized;
            return last.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }

        // Support ** by translating to a regex.
        var regex = "^" + GlobToRegex(pattern) + "$";
        return Regex.IsMatch(normalized, regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string GlobToRegex(string pattern)
    {
        // minimal glob: *, ?, ** across path segments
        // Escape regex metacharacters then restore glob meaning.
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c == '*')
            {
                var isDouble = (i + 1 < pattern.Length && pattern[i + 1] == '*');
                if (isDouble)
                {
                    // ** => any chars, including '/'
                    sb.Append(".*");
                    i++;
                }
                else
                {
                    // * => any chars except '/'
                    sb.Append("[^/]*");
                }
                continue;
            }
            if (c == '?')
            {
                sb.Append("[^/]");
                continue;
            }

            // treat '/' literally
            if ("+()^$.{}!|\\".Contains(c))
                sb.Append('\\').Append(c);
            else if (c == '[' || c == ']')
                sb.Append('\\').Append(c);
            else
                sb.Append(c);
        }
        return sb.ToString();
    }
}
