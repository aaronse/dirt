using System.Globalization;

namespace DirT;

internal enum OutputMode
{
    Indented,
    Paths,
    Json
}

internal sealed class CliOptions
{
    public string? Path { get; private set; }

    public int MaxDepth { get; private set; } = 10;
    public int MaxLines { get; private set; } = 800;
    public bool Trim { get; private set; } = false;
    public bool ShowIgnored { get; private set; } = false;
    public bool Verbose { get; private set; } = false;

    public bool ShowDirs { get; private set; } = true;
    public bool ShowFiles { get; private set; } = true;
    public bool ShowFileSize { get; private set; } = false;

    public OutputMode OutputMode { get; private set; } = OutputMode.Indented;

    public bool UseGitIgnore { get; private set; } = true;

    public PatternSet Excludes { get; private set; } = PatternSet.DefaultExcludes();
    public PatternSet Includes { get; private set; } = PatternSet.EmptyIncludes();

    public bool EmitReadme { get; private set; } = false;
    public string? EmitReadmePath { get; private set; }

    public static CliOptions Parse(string[] args)
    {
        var o = new CliOptions();

        // Support both /key:value and --key value / --key=value
        var i = 0;
        while (i < args.Length)
        {
            var a = args[i];

            if (a is "-h" or "--help" or "/h" or "/?" or "/help")
                throw new CliUsageException("");

            if (!a.StartsWith("/") && !a.StartsWith("--") && o.Path is null)
            {
                o.Path = a;
                i++;
                continue;
            }

            if (a.Equals("/v", StringComparison.OrdinalIgnoreCase) || a.Equals("--verbose", StringComparison.OrdinalIgnoreCase))
            {
                o.Verbose = true;
                i++;
                continue;
            }

            if (a.Equals("/trim", StringComparison.OrdinalIgnoreCase))
            {
                o.Trim = true;
                i++;
                continue;
            }

            if (a.Equals("/show-ignored", StringComparison.OrdinalIgnoreCase))
            {
                o.ShowIgnored = true;
                i++;
                continue;
            }

            if (a.Equals("/dirs", StringComparison.OrdinalIgnoreCase))
            {
                o.ShowFiles = false;
                o.ShowDirs = true;
                i++;
                continue;
            }

            if (a.Equals("/files", StringComparison.OrdinalIgnoreCase))
            {
                o.ShowDirs = false;
                o.ShowFiles = true;
                i++;
                continue;
            }

            if (a.Equals("/size", StringComparison.OrdinalIgnoreCase) || a.Equals("--size", StringComparison.OrdinalIgnoreCase))
            {
                o.ShowFileSize = true;
                i++;
                continue;
            }

            if (a.Equals("/paths", StringComparison.OrdinalIgnoreCase))
            {
                o.OutputMode = OutputMode.Paths;
                i++;
                continue;
            }

            if (a.Equals("/json", StringComparison.OrdinalIgnoreCase))
            {
                o.OutputMode = OutputMode.Json;
                i++;
                continue;
            }

            if (a.Equals("--no-git-ignore", StringComparison.OrdinalIgnoreCase))
            {
                o.UseGitIgnore = false;
                i++;
                continue;
            }

            if (a.StartsWith("/d:", StringComparison.OrdinalIgnoreCase))
            {
                o.MaxDepth = ParseInt(a[3..], "/d:<n>");
                i++;
                continue;
            }

            if (a.Equals("--level", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length) throw new CliUsageException("Missing value for --level.");
                o.MaxDepth = ParseInt(args[i + 1], "--level <n>");
                i += 2;
                continue;
            }

            if (a.StartsWith("/max:", StringComparison.OrdinalIgnoreCase))
            {
                o.MaxLines = ParseInt(a[5..], "/max:<n>");
                i++;
                continue;
            }

            if (a.StartsWith("/x:", StringComparison.OrdinalIgnoreCase))
            {
                o.Excludes = o.Excludes.ApplyDirective(a[3..], DirectiveKind.Exclude);
                i++;
                continue;
            }

            if (a.Equals("--exclude", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length) throw new CliUsageException("Missing value for --exclude.");
                o.Excludes = o.Excludes.ApplyDirective(args[i + 1], DirectiveKind.Exclude);
                i += 2;
                continue;
            }

            if (a.StartsWith("/i:", StringComparison.OrdinalIgnoreCase))
            {
                o.Includes = o.Includes.ApplyDirective(a[3..], DirectiveKind.Include);
                i++;
                continue;
            }

            if (a.Equals("--include", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length) throw new CliUsageException("Missing value for --include.");
                o.Includes = o.Includes.ApplyDirective(args[i + 1], DirectiveKind.Include);
                i += 2;
                continue;
            }

            if (a.Equals("--emit-readme", StringComparison.OrdinalIgnoreCase))
            {
                o.EmitReadme = true;
                // optional path arg if next token is not another switch
                if (i + 1 < args.Length && !IsSwitch(args[i + 1]))
                {
                    o.EmitReadmePath = args[i + 1];
                    i += 2;
                }
                else
                {
                    i += 1;
                }
                continue;
            }

            throw new CliUsageException($"Unknown option: {a}");
        }

        if (o.MaxDepth < -1) throw new CliUsageException("Max depth must be -1 (unlimited) or >= 0.");
        if (o.MaxLines < 10) throw new CliUsageException("Max lines must be >= 10.");

        return o;
    }

    private static bool IsSwitch(string s) => s.StartsWith("/") || s.StartsWith("--");

    private static int ParseInt(string s, string label)
    {
        if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            throw new CliUsageException($"Invalid integer for {label}: {s}");
        return v;
    }
}

internal sealed class CliUsageException : Exception
{
    public CliUsageException(string message) : base(message) { }
}
