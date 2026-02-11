namespace DirT;

internal sealed class TreeBuilder
{
    private readonly IgnoreEngine _ignore;
    private readonly CliOptions _options;
    private int _lines;
    public bool WasTruncated { get; private set; }

    public TreeBuilder(IgnoreEngine ignore, CliOptions options)
    {
        _ignore = ignore;
        _options = options;
    }

    public TreeNode Build(string rootPath)
    {
        _lines = 0;
        WasTruncated = false;

        var root = new TreeNode
        {
            Name = Path.GetFileName(Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) + "/",
            FullPath = Path.GetFullPath(rootPath),
            RelPath = "",
            IsDirectory = true,
            Ignored = false
        };

        // Root name for drive roots like C:\
        if (string.IsNullOrEmpty(root.Name) || root.Name == "/")
            root.Name = root.FullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + "/";

        BuildDir(root, depth: 0);

        return root;
    }

    private void BuildDir(TreeNode node, int depth)
    {
        if (WasTruncated) return;
        if (_options.MaxDepth >= 0 && depth >= _options.MaxDepth) return;

        IEnumerable<string> entries;
        try
        {
            entries = Directory.EnumerateFileSystemEntries(node.FullPath);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            if (_options.Verbose) Console.Error.WriteLine($"[DirT] WARN: cannot access {node.FullPath}: {ex.Message}");
            return;
        }

        var dirs = new List<string>();
        var files = new List<string>();

        foreach (var e in entries)
        {
            if (WasTruncated) break;

            var isDir = Directory.Exists(e);
            var ignored = _ignore.IsIgnored(e, isDir);

            if (ignored)
            {
                if (!isDir && (_options.CountExcludes || _options.ShowAll)
                    && _ignore.TryGetExcludePatternAndLabel(e, isDir: false, out var pat, out var label)
                    && pat != null)
                {
                    AddExcludeCount(node, pat);
                    if (!string.IsNullOrWhiteSpace(label) && !label.Equals(pat, StringComparison.OrdinalIgnoreCase))
                        AddExcludeCount(node, label);
                }

                if (_options.ShowIgnored && isDir && _options.ShowDirs)
                {
                    node.Children.Add(new TreeNode
                    {
                        Name = Path.GetFileName(e) + "/ (ignored)",
                        FullPath = e,
                        RelPath = IgnoreEngine.GetRelativeNormalized(e, node.FullPath),
                        IsDirectory = true,
                        Ignored = true,
                        FileSize = 0
                    });
                    if (CountLine()) { WasTruncated = true; break; }
                }
                continue;
            }

            if (isDir)
            {
                if (_options.ShowDirs) dirs.Add(e);
                else dirs.Add(e); // still traverse even if not shown (for file discovery)
            }
            else
            {
                if (!_ignore.IsIncluded(e, isDir: false)) continue;
                if (_options.ShowFiles) files.Add(e);
            }
        }

        dirs.Sort(StringComparer.OrdinalIgnoreCase);
        files.Sort(StringComparer.OrdinalIgnoreCase);

        // Add directories (and traverse)
        foreach (var d in dirs)
        {
            if (WasTruncated) break;

            var child = new TreeNode
            {
                Name = Path.GetFileName(d) + "/",
                FullPath = d,
                RelPath = IgnoreEngine.GetRelativeNormalized(d, node.FullPath),
                IsDirectory = true,
                FileSize = 0
            };

            // Before adding, optionally decide to collapse (trim) if would exceed budget.
            if (_options.Trim && WouldLikelyOverflowSoon())
            {
                var count = SafeCountItems(d);
                child.Collapsed = true;
                child.CollapsedCount = count;
                node.Children.Add(child);
                if (CountLine()) { WasTruncated = true; break; }
                continue;
            }

            // Traverse to see if it contains anything display-worthy; if not, suppress.
            BuildDir(child, depth + 1);

            if (ShouldKeepDirectory(child))
            {
                if (_options.ShowDirs)
                {
                    node.Children.Add(child);
                    if (CountLine()) { WasTruncated = true; break; }
                }
                else
                {
                    // dirs not shown, but children files need bubbling up? In tree mode, we won't bubble;
                    // we simply keep traversal for deeper file discovery in /paths mode via Children.
                    node.Children.Add(child);
                    if (CountLine()) { WasTruncated = true; break; }
                }
            }
        }

        // Add files
        foreach (var f in files)
        {
            if (WasTruncated) break;
            var fileInfo = new FileInfo(f);
            var child = new TreeNode
            {
                Name = Path.GetFileName(f),
                FullPath = f,
                RelPath = IgnoreEngine.GetRelativeNormalized(f, node.FullPath),
                IsDirectory = false,
                FileSize = fileInfo.Length
            };
            node.Children.Add(child);
            if (CountLine()) { WasTruncated = true; break; }
        }
    }

    private bool ShouldKeepDirectory(TreeNode dir)
    {
        if (dir.Collapsed) return true;
        // keep if any visible children exist
        if (dir.Children.Count > 0) return true;
        // keep if directory has excluded files and user wants to see them
        if (dir.ExcludedTypeCounts.Count > 0 && (_options.ShowAll || _options.ShowIgnored))
            return true;
        return false;
    }

    private bool WouldLikelyOverflowSoon()
    {
        // Heuristic: if we've already used > 70% of budget, start collapsing.
        return _lines >= (int)(_options.MaxLines * 0.7);
    }

    private int SafeCountItems(string dir)
    {
        try
        {
            // Count immediate entries only (cheap, useful)
            return Directory.EnumerateFileSystemEntries(dir).Count();
        }
        catch { return 0; }
    }

    private bool CountLine()
    {
        _lines++;
        return _lines >= _options.MaxLines;
    }

    private void AddExcludeCount(TreeNode node, string pattern)
    {
        if (!node.ExcludedTypeCounts.TryGetValue(pattern, out var v)) v = 0;
        node.ExcludedTypeCounts[pattern] = v + 1;
    }
}
