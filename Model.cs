namespace DirT;

internal sealed class TreeNode
{
    public string Name { get; set; } = "";
    public string FullPath { get; init; } = "";
    public string RelPath { get; init; } = "";
    public bool IsDirectory { get; init; }
    public bool Ignored { get; set; } = false;
    public bool Collapsed { get; set; } = false;
    public int CollapsedCount { get; set; } = 0;
    public long FileSize { get; set; } = 0;

    public List<TreeNode> Children { get; } = new();
}
