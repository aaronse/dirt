namespace DirT;

internal sealed class IgnoreEngine
{
    private readonly string _rootPath;
    private readonly string _rootPathNorm;
    private readonly CliOptions _options;
    private readonly GitContext? _git;
    private readonly GitIgnoreEngine? _gitIgnore;

    public IgnoreEngine(string rootPath, GitContext? git, CliOptions options)
    {
        _rootPath = rootPath;
        _rootPathNorm = NormalizeFullPath(rootPath);
        _git = git;
        _options = options;

        if (git != null && options.UseGitIgnore)
            _gitIgnore = new GitIgnoreEngine(git.IgnoreFiles);
    }

    public bool IsIgnored(string fullPath, bool isDir)
    {
        var name = Path.GetFileName(fullPath);
        if (_options.Excludes.MatchesName(name, isDir)) return true;

        var rel = GetRelativeNormalized(fullPath, _rootPath);
        if (_options.Excludes.MatchesPath(rel, isDir)) return true;

        if (_gitIgnore != null && _git != null)
        {
            // gitignore rules are relative to repo root, not the provided root path
            var repoRel = GetRelativeNormalized(fullPath, _git.RepoRoot);
            if (_gitIgnore.IsIgnored(repoRel, isDir)) return true;
        }

        return false;
    }

    public bool IsIncluded(string fullPath, bool isDir)
    {
        // Includes apply only to files (practical + matches README intent).
        // If includes are empty => include everything.
        if (!_options.Includes.HasAny()) return true;

        if (isDir) return true; // allow traversal; filtering is applied to files and to "empty dir suppression"

        var rel = GetRelativeNormalized(fullPath, _rootPath);
        return _options.Includes.MatchesPath(rel, isDirectory: false) || _options.Includes.MatchesName(Path.GetFileName(fullPath), isDirectory: false);
    }

    public bool TryGetExcludePattern(string fullPath, bool isDir, out string? pattern)
    {
        var name = Path.GetFileName(fullPath);
        var rel = GetRelativeNormalized(fullPath, _rootPath);
        pattern = _options.Excludes.FirstMatch(rel, name, isDir);
        return pattern != null;
    }

    public static string GetRelativeNormalized(string fullPath, string basePath)
    {
        var rel = Path.GetRelativePath(basePath, fullPath);
        rel = rel.Replace('\\', '/');
        if (rel == ".") rel = "";
        return rel.TrimStart('/');
    }

    private static string NormalizeFullPath(string p) => Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
