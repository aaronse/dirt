namespace DirT;

internal static class CliHelp
{
    // Keep this aligned with README; intentionally compact.
    public const string Text = @"
DirT (Dir-T) — output and token-efficient, LLM-friendly directory tree tool

Usage:
  dirt [path] [options]

Core:
  /d:<n>            Max depth; use -1 for unlimited (default) (alias: --level <n>)
  /max:<n>          Max output lines (default: 800)
  /trim             Collapse large dirs to stay within /max
  /show-ignored     Show ignored dirs as placeholders (not expanded)
  /dirs             Show directories only
  /files            Show files only
  /paths            Paths-only output
  /json             JSON output
  /v                Verbose logs to stderr

Exclude:
  /x:<a;b;c>        Replace default excludes
  /x:+<a;b;c>       Add to default excludes
  /x:-<a;b;c>       Remove from default excludes
  --exclude <exprs> Same as /x:

Include:
  /i:<a;b;c>        Replace include filters
  /i:+<a;b;c>       Add to include filters
  /i:-<a;b;c>       Remove from include filters
  --include <exprs> Same as /i:

Gitignore:
  --no-git-ignore   Ignore .gitignore rules (still applies DirT defaults)

README:
  --emit-readme [path]  Write embedded README.md to disk and exit

Exit codes:
  0 success
  2 output truncated by /max (still produced useful output)
  64 usage error
";
}
