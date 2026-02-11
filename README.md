# DirT(ree)

**DirT — Displays Tree of files and subdirs.  Token-efficient LLM-friendly output.  Sensible filters by default.**

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

Unlimited depth by default; use /max and/or /trim to prevent runaway output.

Show excluded file counts (per directory):

```
/count
```

Limit depth:

```
/d:3
--level 3
```

Limit total output lines (safety guard):

```
/max:500
```

**Note:** If the output is truncated due to the `/max` limit, DirT will:
- Return exit code 2 (still producing useful partial output)
- Display a warning message to stderr with the exact command to re-run with doubled limit
- Example: `dirt /max:1000` (if original was `/max:500`)

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

### Typical "LLM context" dump

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
* "key files" mode (entry points only)
* README auto-emit (`dirt --emit-readme`)

---

## License

TBD (project decision)
