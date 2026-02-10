namespace DirT;

internal static class TreeRender
{
    private static string FormatFileSize(long bytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        if (bytes >= GB)
            return (bytes / (double)GB).ToString("F1") + " GB";
        if (bytes >= MB)
            return (bytes / (double)MB).ToString("F1") + " MB";
        if (bytes >= KB)
            return (bytes / (double)KB).ToString("F0") + " KB";
        return bytes + " B";
    }

    public static IEnumerable<string> RenderIndented(TreeNode root, CliOptions options)
    {
        // Render children of root without forcing an extra synthetic root if the user provided a path.
        // But showing the root helps orientation; keep it.
        yield return NormalizeForOutput(root.Name, isDirectory: true);

        if (options.CountExcludes)
        {
            foreach (var line in RenderExcludeCounts(root, depth: 1))
                yield return line;
        }

        foreach (var line in RenderIndentedInternal(root, options, depth: 1))
            yield return line;
    }

    private static IEnumerable<string> RenderIndentedInternal(TreeNode node, CliOptions options, int depth)
    {
        var indent = new string(' ', depth * 2);

        // dirs first then files already in builder
        foreach (var c in node.Children)
        {
            if (c.Ignored)
            {
                yield return indent + c.Name;
                continue;
            }

            if (c.IsDirectory)
            {
                if (c.Collapsed)
                {
                    yield return indent + $"{c.Name.TrimEnd('/')}/ … ({c.CollapsedCount} items)";
                    continue;
                }

                if (options.ShowDirs)
                {
                    yield return indent + NormalizeForOutput(c.Name, isDirectory: true);

                    if (options.CountExcludes)
                    {
                        foreach (var line in RenderExcludeCounts(c, depth + 1))
                            yield return line;
                    }
                }

                foreach (var line in RenderIndentedInternal(c, options, depth + 1))
                    yield return line;
            }
            else
            {
                if (!options.ShowFiles) continue;
                var sizeSuffix = options.ShowFileSize ? $" [{FormatFileSize(c.FileSize)}]" : "";
                yield return indent + c.Name + sizeSuffix;
            }
        }
    }

    private static IEnumerable<string> RenderExcludeCounts(TreeNode node, int depth)
    {
        if (node.ExcludedTypeCounts.Count == 0) yield break;

        var indent = new string(' ', depth * 2);
        foreach (var kv in node.ExcludedTypeCounts
                     .OrderByDescending(k => k.Value)
                     .ThenBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            yield return indent + $"{kv.Value} {kv.Key}";
        }
    }

    public static IEnumerable<string> RenderPaths(TreeNode root, CliOptions options)
    {
        // In paths mode, output relative paths from the displayed root.
        // Keep POSIX separators for token efficiency.
        foreach (var p in RenderPathsInternal(root, prefix: "", options))
            yield return p;
    }

    private static IEnumerable<string> RenderPathsInternal(TreeNode node, string prefix, CliOptions options)
    {
        // prefix does NOT include the node.Name itself; we build it.
        foreach (var c in node.Children)
        {
            if (c.Ignored)
            {
                if (options.ShowIgnored)
                {
                    var name = c.Name.Replace(" (ignored)", "");
                    var rel = (prefix + name).Replace('\\', '/');
                    yield return rel;
                }
                continue;
            }

            if (c.IsDirectory)
            {
                var rel = (prefix + c.Name).Replace('\\', '/');
                if (options.ShowDirs) yield return rel;

                if (c.Collapsed) continue;

                foreach (var p in RenderPathsInternal(c, rel, options))
                    yield return p;
            }
            else
            {
                if (!options.ShowFiles) continue;
                var rel = (prefix + c.Name).Replace('\\', '/');
                var sizeSuffix = options.ShowFileSize ? $" [{FormatFileSize(c.FileSize)}]" : "";
                yield return rel + sizeSuffix;
            }
        }
    }

    private static string NormalizeForOutput(string name, bool isDirectory)
    {
        if (isDirectory && !name.EndsWith("/")) return name + "/";
        return name.Replace('\\', '/');
    }
}
