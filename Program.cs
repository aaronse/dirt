using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DirT;

internal static class Program
{
    private const string EmbeddedReadme = """
# DirT

**DirT (pronounced “Dir-T”) — output and token-efficient, LLM-friendly directory tree tool**

DirT prints a clean, minimal directory tree optimized for:

* human scanning
* LLM context sharing
* low token usage
* developer workflows

It is **`.gitignore`-aware by default**, ships with **sensible dev excludes**, and favors **indentation over symbols**.

---

## Why DirT exists

Most tree tools optimize for terminals.
DirT optimizes for **communication**.

Goals:

* Reduce irrelevant noise (`bin/`, `obj/`, `node_modules/`, etc.)
* Produce output that LLMs parse correctly on first pass
* Avoid token waste (icons, glyphs, metadata)
* Require little to no manual cleanup before sharing

---

## Default behavior

* Git-aware:

  * Honors `.gitignore`
  * Honors `.git/info/exclude`
  * Honors global gitignore (if available)
* Built-in dev excludes (unless overridden):

  ```
  bin/
  obj/
  node_modules/
  .git/
  .vs/
  .idea/
  dist/
  build/
  out/
  target/
  coverage/
  .cache/
  packages/
  ```
* Directories are listed **before files**
* Directories end with `/`
* Uses **indentation only** (no box-drawing characters)
* Suppresses directories that contain *only* ignored content

---

## Output format (default)

Minimal, indentation-driven tree:

```
src/
  App/
    AppHost.cs
  Forms/
  Utils/
  WebUI/
  Program.cs
tests/
docs/
```

Rules:

* Trailing `/` = directory
* No suffix = file
* 2 spaces per indent level
* Sorted: directories first, then files

This format is **LLM-safe**, **diff-friendly**, and **token-efficient**.

---

## Usage

```
dirt [path] [options]
```

If no path is provided, defaults to the current directory.

---

## Common options

### Exclude rules

Replace defaults:

```
/x:bin;obj;node_modules
```

Add to defaults:

```
/x:+dist;coverage;*.min.js
```

Remove from defaults:

```
/x:-node_modules
```

Rules support simple glob patterns.

---

### Include filters

Only include matching files:

```
/i:*.cs;*.csproj;*.md;*.json
```

Similar to Exclude, add to defaults /i:+... and Remove from defaults /i:-... can be used.

---

### Depth and output control

Limit depth:

```
/d:3
--level 3
```

Limit total output lines (safety guard):

```
/max:500
```

Show excluded file counts (per directory):

```
/count
```

Collapse large directories automatically:

```
/trim
```

Collapsed output example:

```
WebUI/
  libs/ … (42 files)
```

---

### Ignored content visibility

Show ignored directories as placeholders:

```
/show-ignored
```

Example:

```
node_modules/ (ignored)
```

Ignored directories are still **not expanded** unless explicitly included.

---

## Output modes

Indented tree (default):

```
dirt
```

Paths-only (maximum compression):

```
dirt /paths
```

JSON (tooling / automation):

```
dirt /json
```

---

## Familiar flag aliases

DirT intentionally mirrors common tools where possible.

| Purpose      | DirT            | Alias           |
| ------------ | --------------- | --------------- |
| Max depth    | `/d:3`          | `--level 3`     |
| Exclude      | `/x:`           | `--exclude`     |
| Include      | `/i:`           | `--include`     |
| Show ignored | `/show-ignored` | —               |
| Verbose logs | `/v`            | —               |
| ASCII only   | default         | `/ascii` (noop) |

---

## Examples

### Typical “LLM context” dump

```
dirt src /d:4 /i:*.cs;*.csproj;*.md
```

### Debug build artifacts

```
dirt /x:-bin;-obj /show-ignored
```

### Count excluded file types (per directory)

```
dirt /ah /x:*.jpeg;*.json;*.png*;*.mp4;*.webm;*.mkv /show-ignored /count
```

### Ultra-compact overview

```
dirt /dirs /d:3
```

### Git-respecting file list

```
dirt /paths /i:*.cs
```

---

## Design principles (non-negotiable)

* **Indentation beats glyphs**
* **Paths beat metadata**
* **Ignored noise stays hidden**
* **Output must be copy-paste safe**
* **Defaults must be sane for devs**
* **LLMs should not need explanation**

---

## Roadmap (non-binding)

* `.dirtignore` support
* language-aware presets (`/preset:csharp`, `/preset:web`)
* “key files” mode (entry points only)
* README auto-emit (`dirt --emit-readme`)

---

## License

TBD (project decision)

""";

    public static int Main(string[] args)
    {
        try
        {
            var options = CliOptions.Parse(args);

            if (options.EmitReadme)
            {
                var outPath = options.EmitReadmePath ?? Path.Combine(Environment.CurrentDirectory, "README.md");
                File.WriteAllText(outPath, EmbeddedReadme, Encoding.UTF8);
                if (options.Verbose) Console.Error.WriteLine($"[DirT] Wrote README to: {outPath}");
                return 0;
            }

            var rootPath = options.Path ?? Environment.CurrentDirectory;
            rootPath = Path.GetFullPath(rootPath);

            var repo = GitContext.TryCreate(rootPath, options);
            var ignore = new IgnoreEngine(rootPath, repo, options);

            var builder = new TreeBuilder(ignore, options);
            var tree = builder.Build(rootPath);

            if (options.OutputMode == OutputMode.Json)
            {
                var json = JsonSerializer.Serialize(tree, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                Console.WriteLine(json);
                if (builder.WasTruncated)
                    EmitTruncationWarning(args, options.MaxLines);
                return builder.WasTruncated ? 2 : 0;
            }

            if (options.OutputMode == OutputMode.Paths)
            {
                foreach (var p in TreeRender.RenderPaths(tree, options))
                    Console.WriteLine(p);
                if (builder.WasTruncated)
                    EmitTruncationWarning(args, options.MaxLines);
                return builder.WasTruncated ? 2 : 0;
            }

            foreach (var line in TreeRender.RenderIndented(tree, options))
                Console.WriteLine(line);

            if (builder.WasTruncated)
                EmitTruncationWarning(args, options.MaxLines);

            return builder.WasTruncated ? 2 : 0;
        }
        catch (CliUsageException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine();
            Console.Error.WriteLine(CliHelp.Text);
            return 64; // EX_USAGE
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[DirT] ERROR: {ex.Message}");
            if (args.Contains("/v", StringComparer.OrdinalIgnoreCase)) Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void EmitTruncationWarning(string[] args, int currentMaxLines)
    {
        var newMaxLines = currentMaxLines * 2;
        var retryCommand = BuildRetryCommand(args, currentMaxLines, newMaxLines);
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Error.WriteLine();
        Console.Error.WriteLine($"[DirT] WARNING: Output truncated at {currentMaxLines} lines (max limit reached)");
        Console.Error.WriteLine($"[DirT] The directory tree is larger than the configured limit.");
        Console.Error.WriteLine($"[DirT] To see more content, re-run with increased limit:");
        Console.Error.WriteLine();
        Console.Error.WriteLine($"  {retryCommand}");
        Console.Error.WriteLine();
        Console.ResetColor();
    }

    private static string BuildRetryCommand(string[] args, int oldMaxLines, int newMaxLines)
    {
        var newArgs = new List<string>();
        var maxLinesSeen = false;

        foreach (var arg in args)
        {
            if (arg.StartsWith("/max:", StringComparison.OrdinalIgnoreCase))
            {
                newArgs.Add($"/max:{newMaxLines}");
                maxLinesSeen = true;
            }
            else
            {
                newArgs.Add(arg);
            }
        }

        if (!maxLinesSeen)
        {
            newArgs.Add($"/max:{newMaxLines}");
        }

        return "dirt " + string.Join(" ", newArgs.Select(a => a.Contains(' ') ? $"\"{a}\"" : a));
    }
}
