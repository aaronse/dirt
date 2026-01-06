using System.Diagnostics;

namespace DirT;

internal sealed class GitContext
{
    public string RepoRoot { get; }
    public IReadOnlyList<string> IgnoreFiles { get; }

    private GitContext(string repoRoot, List<string> ignoreFiles)
    {
        RepoRoot = repoRoot;
        IgnoreFiles = ignoreFiles;
    }

    public static GitContext? TryCreate(string startPath, CliOptions options)
    {
        if (!options.UseGitIgnore) return null;

        // Locate repo root by walking up looking for .git directory
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            var gitDir = Path.Combine(dir.FullName, ".git");
            if (Directory.Exists(gitDir))
            {
                var ignoreFiles = new List<string>();
                var rootGitIgnore = Path.Combine(dir.FullName, ".gitignore");
                if (File.Exists(rootGitIgnore)) ignoreFiles.Add(rootGitIgnore);

                var infoExclude = Path.Combine(gitDir, "info", "exclude");
                if (File.Exists(infoExclude)) ignoreFiles.Add(infoExclude);

                // Global excludes file (best-effort): git config --get core.excludesfile
                var global = TryGetGitGlobalExcludesFile();
                if (!string.IsNullOrWhiteSpace(global) && File.Exists(global)) ignoreFiles.Add(global);

                return new GitContext(dir.FullName, ignoreFiles);
            }
            dir = dir.Parent;
        }
        return null;
    }

    private static string? TryGetGitGlobalExcludesFile()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "config --get core.excludesfile",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p == null) return null;
            var output = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit(1000);
            if (string.IsNullOrWhiteSpace(output)) return null;

            // Expand ~
            if (output.StartsWith("~"))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                output = Path.Combine(home, output.TrimStart('~', '/', '\\'));
            }
            return output;
        }
        catch
        {
            return null;
        }
    }
}

internal sealed class GitIgnoreRule
{
    public string Pattern { get; }
    public bool IsNegation { get; }
    public bool DirOnly { get; }
    public bool Rooted { get; }

    public GitIgnoreRule(string pattern, bool isNegation, bool dirOnly, bool rooted)
    {
        Pattern = pattern;
        IsNegation = isNegation;
        DirOnly = dirOnly;
        Rooted = rooted;
    }
}

internal sealed class GitIgnoreEngine
{
    private readonly List<GitIgnoreRule> _rules = new();

    public GitIgnoreEngine(IEnumerable<string> ignoreFiles)
    {
        foreach (var f in ignoreFiles)
            Load(f);
    }

    private void Load(string filePath)
    {
        foreach (var raw in File.ReadAllLines(filePath))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("#")) continue;

            var neg = line.StartsWith("!");
            if (neg) line = line[1..];

            var dirOnly = line.EndsWith("/");
            if (dirOnly) line = line.TrimEnd('/');

            var rooted = line.StartsWith("/");
            if (rooted) line = line.TrimStart('/');

            // Normalize
            line = line.Replace('\\', '/');

            _rules.Add(new GitIgnoreRule(line, neg, dirOnly, rooted));
        }
    }

    // Simplified gitignore semantics:
    // - Applies rules in order; last match wins.
    // - Rooted patterns match from repo root; otherwise match against relative path OR any segment.
    public bool IsIgnored(string relPath, bool isDir)
    {
        relPath = relPath.Replace('\\', '/').TrimStart('/');
        var ignored = false;

        foreach (var r in _rules)
        {
            if (r.DirOnly && !isDir) continue;

            var matched = false;
            if (r.Rooted)
            {
                matched = GlobMatcher.IsMatch(relPath, r.Pattern, isDir);
            }
            else
            {
                // match full path OR segment
                matched = GlobMatcher.IsMatch(relPath, r.Pattern, isDir);
                if (!matched && !r.Pattern.Contains('/'))
                {
                    var segments = relPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    matched = segments.Any(s => GlobMatcher.IsMatch(s, r.Pattern, isDir));
                }
            }

            if (matched)
                ignored = !r.IsNegation;
        }

        return ignored;
    }
}
