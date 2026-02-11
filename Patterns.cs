using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace DirT;

internal enum DirectiveKind { Exclude, Include }

internal sealed class PatternSet
{
    public IReadOnlyList<string> Patterns => _patterns;
    private readonly List<string> _patterns;
    private readonly Dictionary<string, string> _patternLabels;

    // Predefined token definitions for common file type groups
    private static readonly Dictionary<string, string> TokenDefinitions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["media"] = "*.jpg;*.jpeg;*.png;*.png*;*.gif;*.bmp;*.svg;*.webp;*.ico;*.tiff;*.tif;" +
                    "*.mp4;*.avi;*.mkv;*.webm;*.mov;*.flv;*.wmv;*.m4v;*.mpg;*.mpeg;" +
                    "*.mp3;*.wav;*.flac;*.aac;*.ogg;*.m4a;*.wma;*.opus",
        
        ["code"] = "*.cs;*.csproj;*.sln;*.vb;*.vbproj;*.fs;*.fsproj;" +
                   "*.js;*.ts;*.jsx;*.tsx;*.json;*.mjs;*.cjs;" +
                   "*.py;*.pyc;*.pyo;*.pyd;" +
                   "*.java;*.class;*.jar;*.war;" +
                   "*.cpp;*.c;*.h;*.hpp;*.cc;*.cxx;*.hxx;" +
                   "*.go;*.rs;*.rb;*.php;*.swift;*.kt;*.kts;*.scala;*.clj;*.cljs;" +
                   "*.r;*.m;*.mm;*.sh;*.bat;*.ps1;*.cmd;*.pl;*.lua;*.asm;*.s",
        
        ["docs"] = "*.md;*.txt;*.pdf;*.doc;*.docx;*.odt;*.rtf;*.tex;*.rst;" +
                   "*.adoc;*.asciidoc;*.html;*.htm;*.xml;*.xhtml",
        
        ["data"] = "*.json;*.xml;*.yaml;*.yml;*.toml;*.ini;*.cfg;*.conf;" +
                   "*.csv;*.tsv;*.dat;" +
                   "*.sql;*.db;*.sqlite;*.sqlite3;*.mdb;*.accdb;" +
                   "*.parquet;*.avro;*.jsonl;*.ndjson",
        
        ["archive"] = "*.zip;*.rar;*.7z;*.tar;*.gz;*.bz2;*.xz;*.lz;*.lzma;" +
                      "*.tgz;*.tbz2;*.txz;*.zipx;*.cab;*.iso;*.dmg"
    };

    private static readonly Regex TokenRegex = new(@"^\{(\w+)\}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly record struct PatternEntry(string Pattern, string Display);

    private PatternSet(IEnumerable<string> patterns)
        : this(patterns.Select(p => new PatternEntry(p, p)))
    {
    }

    private PatternSet(IEnumerable<PatternEntry> entries)
    {
        _patterns = new List<string>();
        _patternLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Pattern)) continue;
            _patterns.Add(entry.Pattern);
            _patternLabels[entry.Pattern] = entry.Display;
        }
    }

    public static PatternSet DefaultExcludes() => new(new[]
    {
        "bin", "obj", "node_modules", ".git", ".vs", ".idea",
        "dist", "build", "out", "target", "coverage", ".cache",
        "packages"
    });

    // Empty includes means "include everything"
    public static PatternSet EmptyIncludes() => new(Array.Empty<string>());

    /// <summary>
    /// Expands tokens like {media}, {code}, etc. into pattern entries with a display label.
    /// Tokens are case-insensitive. Unknown tokens are treated as literal patterns.
    /// </summary>
    /// <param name="exprs">Semicolon-delimited expressions, may include tokens like {media}</param>
    /// <returns>Expanded entries with display labels for counting</returns>
    private static IEnumerable<PatternEntry> ExpandTokenEntries(string exprs)
    {
        foreach (var item in SplitExprList(exprs))
        {
            var match = TokenRegex.Match(item);
            if (match.Success)
            {
                var tokenName = match.Groups[1].Value;
                if (tokenName.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var token in TokenDefinitions.Keys
                                 .Where(k => !k.Equals("all", StringComparison.OrdinalIgnoreCase))
                                 .OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                    {
                        var label = "{" + token.ToLowerInvariant() + "}";
                        foreach (var pat in SplitExprList(TokenDefinitions[token]))
                            yield return new PatternEntry(pat, label);
                    }
                    continue;
                }
                if (TokenDefinitions.TryGetValue(tokenName, out var expansion))
                {
                    var label = "{" + tokenName.ToLowerInvariant() + "}";
                    foreach (var pat in SplitExprList(expansion))
                        yield return new PatternEntry(pat, label);
                    continue;
                }
            }

            yield return new PatternEntry(item, item);
        }
    }

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

        // Expand tokens like {media} into pattern entries before applying mode
        var items = ExpandTokenEntries(trimmed);

        if (mode == 'r')
            return new PatternSet(items);

        if (mode == '+')
        {
            var merged = new Dictionary<string, string>(_patternLabels, StringComparer.OrdinalIgnoreCase);
            foreach (var entry in items)
                merged[entry.Pattern] = entry.Display;

            return new PatternSet(merged.Select(kv => new PatternEntry(kv.Key, kv.Value)));
        }

        // mode == '-'
        {
            var remove = new HashSet<string>(items.Select(i => i.Pattern), StringComparer.OrdinalIgnoreCase);
            var kept = _patterns.Where(p => !remove.Contains(p))
                .Select(p => new PatternEntry(p, _patternLabels[p]));
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

    public bool TryGetFirstMatchWithLabel(string relativePath, string name, bool isDirectory, out string? pattern, out string? label)
    {
        pattern = FirstMatch(relativePath, name, isDirectory);
        if (pattern == null)
        {
            label = null;
            return false;
        }

        label = _patternLabels.TryGetValue(pattern, out var display) ? display : pattern;
        return true;
    }
}

internal static class GlobMatcher
{
    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new(StringComparer.Ordinal);

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

        // Support ** by translating to a regex (cache compiled regex per pattern).
        var regex = RegexCache.GetOrAdd(pattern, p =>
            new Regex("^" + GlobToRegex(p) + "$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled));
        return regex.IsMatch(normalized);
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
