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
  /size             Show approximate file sizes in brackets after file names
  /count, /c        Show per-directory excluded file counts by pattern
  /dirs             Show directories only
  /files            Show files only
  /paths            Paths-only output
  /json             JSON output
  /v                Verbose logs to stderr
  /ah               Show all files, including those ignored by .gitignore (implies --no-git-ignore)

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

Tokens:
  Use predefined tokens wrapped in {} for common file type groups (case-insensitive).
  Tokens can be mixed with patterns and used with +/- modifiers.

  Available tokens:
    {media}         Images, video, audio (jpg, png, gif, mp4, mp3, wav, etc.)
    {code}          Source code files (cs, js, py, java, cpp, go, rs, etc.)
    {docs}          Documentation (md, txt, pdf, doc, html, etc.)
    {data}          Data files (json, xml, yaml, csv, sql, db, etc.)
    {archive}       Compressed files (zip, rar, 7z, tar, gz, etc.)
    {all}           Shorthand for {media}+{code}+{docs}+{data}+{archive}

  Examples:
    dirt /x:{media}                 Exclude all media files
    dirt /x:{media};{data}          Exclude media and data files
    dirt /x:{media};temp*           Exclude media files and temp* patterns
    dirt /x:+{code}                 Add code files to default excludes
    dirt /x:{MEDIA}                 Case-insensitive (same as {media})
    dirt /x:{all}                   Exclude all token file groups

Gitignore:
  --no-git-ignore   Ignore .gitignore rules (still applies DirT defaults)

README:
  --emit-readme [path]  Write embedded README.md to disk and exit

Exit codes:
  0 success
  2 output truncated by /max (still produced useful output; see stderr for retry command)
  64 usage error
";
}
