namespace DirT;

internal sealed class TreeNode
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public string RelPath { get; set; } = "";
    public bool IsDirectory { get; set; }
    public bool Ignored { get; set; }
    public long FileSize { get; set; }
    public List<TreeNode> Children { get; } = new();
    public bool Collapsed { get; set; }
    public int CollapsedCount { get; set; }
    public Dictionary<string, int> ExcludedTypeCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, long> ExcludedTypeSizes { get; } = new(StringComparer.OrdinalIgnoreCase);
}