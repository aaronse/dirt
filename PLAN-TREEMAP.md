# DirT - Directory Tree Analyzer

## Project Overview

DirT (Directory Tree) is a CLI tool designed to help users and LLMs understand a project's directory structure and storage usage through interactive treemap visualizations. It provides both text-based directory analysis and modern web-based treemap visualizations for intuitive exploration of disk space usage.

## Design Goals

1. **Clarity for Humans & LLMs**: Generate clear, parseable directory structure output
2. **Storage Visibility**: Visualize disk space usage patterns through interactive treemaps
3. **Fast & Efficient**: Quick analysis even for large directory trees
4. **Minimal Distribution Size**: Single, small binary with no runtime dependencies
5. **Modern Visualization**: Leverage D3.js for interactive, zoomable treemaps
6. **Fast Dev Inner Loop**: Hot reload during development for rapid iteration

## Architecture

### Core Components

#### 1. CLI Interface (.NET 8)
- **Purpose**: Command-line argument parsing and orchestration
- **Key Features**:
  - Zero arguments → ASCII tree of current directory
  - `/t` flag → Treemap visualization in browser
  - Path specification
  - Filtering and exclusion patterns
  - Depth limiting

#### 2. Directory Scanner (.NET 8)
- **Purpose**: Traverse filesystem and collect structure/size data
- **Key Features**:
  - Recursive directory traversal using `Directory.EnumerateFileSystemEntries`
  - File size aggregation using `FileInfo.Length`
  - Configurable filtering (ignore node_modules, .git, etc.)
  - Symlink handling with `FileAttributes.ReparsePoint`
  - Permission error handling with try-catch
  - Fast native filesystem APIs

#### 3. ASCII Tree Formatter (.NET 8)
- **Purpose**: Generate human/LLM-readable directory tree output
- **Key Features**:
  - ASCII tree visualization using box drawing characters
  - File size display (human-readable or bytes)
  - File count summaries
  - Color coding using `Console.ForegroundColor` (optional)
  - Summary statistics

#### 4. Data Serializer (.NET 8)
- **Purpose**: Convert directory structure to compact JSON for web UI
- **Key Features**:
  - Serialize to JSON using `System.Text.Json`
  - Compact representation (minimal metadata)
  - Write to temp file (overwrite existing)
  - Clean contract for web UI consumption
  - Single instance temp file (no multi-instance support needed)

#### 5. Web Server (.NET 8)
- **Purpose**: Serve treemap HTML and data using built-in Kestrel
- **Key Features**:
  - Minimal Kestrel web server
  - Serve static HTML/JS/CSS
  - Serve JSON data endpoint
  - Auto-launch default browser
  - Auto-shutdown after serving
  - Hot reload during development

#### 6. Treemap Web UI (HTML/JS)
- **Purpose**: Interactive visualization in browser
- **Key Features**:
  - D3.js-based zoomable treemap
  - Color-coded by file type/extension
  - Size-proportional rectangles
  - Interactive breadcrumb navigation
  - Hover tooltips with details
  - Click to zoom functionality
  - Load data from JSON endpoint

### Data Flow

```
CLI (dirt.exe) → Directory Scanner → Data Structure
                                          ↓
                                    ┌─────┴─────┐
                                    ↓           ↓
                            ASCII Formatter  JSON Serializer
                                    ↓           ↓
                                 Console    Temp JSON File
                                                ↓
                                         Kestrel Server
                                                ↓
                                         Launch Browser
                                                ↓
                                         D3.js Treemap UI
```

## Key Design Decisions

### Technology Choices

#### Why .NET 8?
- **Native AOT**: Single-file, self-contained binaries with minimal size
- **No Runtime Required**: Users don't need .NET installed
- **Fast Startup**: Sub-second startup with Native AOT
- **Built-in Web Server**: Kestrel included, no external dependencies
- **Cross-platform**: Works on Windows, macOS, Linux
- **Hot Reload**: Fast development inner loop with `dotnet watch`
- **Modern C#**: Clean, productive language with excellent tooling
- **System APIs**: Direct access to native filesystem APIs

#### Why NOT Node.js?
- **Distribution Size**: Node.js requires full runtime (~50MB+)
- **Dependencies**: npm packages add significant bloat
- **Startup Time**: V8 initialization overhead
- **Binary Distribution**: Requires packaging with pkg/nexe with compromises
- **.NET Advantage**: Native AOT produces 10-30MB self-contained executables

#### Why Built-in Kestrel?
- **Zero Dependencies**: Included in .NET runtime
- **Lightweight**: Minimal overhead for simple serving
- **Fast**: High-performance HTTP server
- **Easy**: Simple API for static file serving
- **No External Server**: No need for IIS, Apache, nginx

#### Why D3.js for Treemaps?
- **Industry standard** for data visualization
- **Treemap layouts** built-in and optimized
- **Highly interactive** with zoom, pan, hover
- **Customizable** appearance and behavior
- **No backend required** - pure client-side rendering

### Architecture Decisions

#### Single Binary Distribution
- **Decision**: PublishAot with SelfContained=true
- **Why**: Minimal user setup, fast execution, small size
- **Trade-off**: Longer build time vs. superior UX
- **Result**: Single .exe on Windows, single binary on Linux/macOS

#### Temp File for Data Contract
- **Decision**: Write directory data to single temp file (JSON)
- **Why**: Clean separation between CLI and web UI, testable contract
- **Trade-off**: Disk I/O vs. clean architecture
- **Result**: Web UI loads from well-defined JSON endpoint
- **Location**: `Path.GetTempPath() + "dirt-data.json"`
- **Behavior**: Overwrite existing file, single instance

#### Hot Reload Strategy
- **Decision**: Use `dotnet watch` during development
- **Why**: Fast inner loop, see changes immediately
- **Trade-off**: Slight memory overhead vs. developer productivity
- **Result**: Save file → automatic rebuild → instant feedback

#### Synchronous vs Async File Operations
- **Decision**: Async (async/await)
- **Why**: Non-blocking for large directory trees
- **Trade-off**: Slightly more complex code vs. better performance
- **Result**: Responsive CLI even during long scans

#### In-Memory Data Structure
- **Decision**: Build complete tree in memory before output
- **Why**: Enables size aggregation, sorting, multiple output formats
- **Trade-off**: Memory usage for huge directories vs. flexibility
- **Result**: Fast post-processing and multiple format support
- **Future**: Streaming mode for extremely large trees

#### Filtering Strategy
- **Decision**: Configurable exclusion patterns with sensible defaults
- **Why**: Skip irrelevant files (node_modules, .git, build artifacts)
- **Trade-off**: Might miss files vs. cleaner, faster output
- **Result**: Default patterns for common scenarios, user overridable

#### Browser Launch
- **Decision**: Auto-launch default browser after server starts
- **Why**: Seamless UX, user doesn't need to manually open URL
- **Trade-off**: Assumes user wants browser vs. minimal friction
- **Result**: Execute command → see visualization

#### Server Lifetime
- **Decision**: Auto-shutdown after serving page
- **Why**: No lingering processes, clean resource usage
- **Trade-off**: Can't refresh page vs. simplicity
- **Result**: One-shot visualization, rerun for updates

## CLI Design

### Command Structure

#### Generate ASCII Tree (Default)
```bash
# Current directory
dirt.exe

# Specific path
dirt.exe C:\projects\myapp

# With depth limit
dirt.exe --depth 3

# With exclusions
dirt.exe --exclude "*.log,bin/*,obj/*"
```

**Output**: ASCII tree to console
```
📁 myapp/
├── 📁 src/
│   ├── 📄 Program.cs (1.2 KB)
│   └── 📄 Utils.cs (3.4 KB)
├── 📁 test/
│   └── 📄 Tests.cs (2.1 KB)
└── 📄 README.md (4.5 KB)

Total: 4 files, 11.2 KB
```

#### Generate Treemap Visualization
```bash
# Current directory
dirt.exe /t

# Specific path
dirt.exe /t C:\projects\myapp

# With options
dirt.exe /t --exclude "bin/*,obj/*" --min-size 1MB
```

**Behavior**:
1. Scan directory structure
2. Serialize to `%TEMP%\dirt-data.json` (overwrite existing)
3. Start Kestrel server on localhost:5000
4. Launch default browser to `http://localhost:5000`
5. Serve treemap HTML/JS
6. Web UI loads JSON from `/data` endpoint
7. Auto-shutdown server after initial page load (configurable)

### Options

#### Path Arguments
- `[path]`: Directory to analyze (default: current directory)

#### Mode Selection
- `/t`: Generate treemap visualization in browser (alias for `--treemap`)
- `--treemap`: Generate treemap visualization
- (no flag): Generate ASCII tree (default)

#### Filtering
- `--exclude <patterns>`: Comma-separated exclusion patterns
- `--include <patterns>`: Only include matching patterns
- `--depth <n>`: Maximum depth to traverse
- `--min-size <size>`: Minimum size to display (e.g., 1MB, 500KB)

#### Display Options
- `--sizes`: Show file sizes (default for ASCII)
- `--no-sizes`: Hide file sizes
- `--sort <method>`: Sort by name, size, type
- `--human`: Human-readable sizes (default)
- `--bytes`: Show sizes in bytes

#### Advanced
- `--follow-symlinks`: Follow symbolic links
- `--hidden`: Include hidden files
- `--port <n>`: Web server port (default: 5000)
- `--no-launch`: Don't auto-launch browser
- `--keep-server`: Don't auto-shutdown server

## Data Contract

### JSON Format (dirt-data.json)

Minimal, compact representation for web UI consumption:

```json
{
  "root": {
    "name": "myapp",
    "path": "C:\\projects\\myapp",
    "size": 1048576,
    "children": [
      {
        "name": "src",
        "path": "src",
        "size": 524288,
        "children": [
          {
            "name": "Program.cs",
            "path": "src/Program.cs",
            "size": 1234,
            "type": "file",
            "ext": ".cs"
          }
        ]
      }
    ]
  },
  "summary": {
    "totalFiles": 42,
    "totalSize": 1048576,
    "totalDirs": 8,
    "scannedAt": "2025-01-27T10:30:00Z"
  }
}
```

### Web UI Endpoints

- `GET /`: Serves treemap.html
- `GET /data`: Returns dirt-data.json
- `GET /static/*`: Serves CSS, JS (embedded in HTML for simplicity)

## Treemap Visualization

### Features

#### Interactive Elements
1. **Zoom Navigation**: Click rectangles to zoom in
2. **Breadcrumbs**: Click path segments to navigate up
3. **Hover Tooltips**: Show full path, size, percentage
4. **Size Display**: Each rectangle labeled with name and size
5. **Color Coding**: Different colors for file types/extensions
6. **Responsive**: Adapts to window size

#### Visual Design
- **Color Scheme**: 
  - Code files (.cs, .js, .py): Blue tones
  - Images (.png, .jpg): Green tones
  - Documents (.md, .txt): Yellow tones
  - Archives (.zip, .tar): Red tones
  - Other: Gray tones
- **Layout**: Squarified treemap algorithm (balanced aspect ratios)
- **Sizing**: Rectangle area proportional to file/directory size
- **Labels**: Visible for reasonably-sized rectangles only

#### Technical Implementation
```javascript
// D3.js treemap with zoom
const treemap = d3.treemap()
  .size([width, height])
  .padding(1)
  .round(true);

// Load data from endpoint
fetch('/data')
  .then(r => r.json())
  .then(data => {
    const root = d3.hierarchy(data.root)
      .sum(d => d.size)
      .sort((a, b) => b.size - a.size);
    
    treemap(root);
    renderTreemap(root);
  });
```

## Hot Reload Development

### Setup
```bash
# Watch mode with hot reload
dotnet watch run

# Make changes to C# code → auto-rebuild → auto-restart
# Make changes to HTML/JS → manually refresh browser (or use browser dev tools)
```

### Benefits
- **Fast Feedback**: See changes in < 1 second
- **No Manual Rebuild**: Automatic on file save
- **Preserves State**: Can maintain breakpoints in debugger
- **Efficient**: Only rebuilds changed files

## Verification Strategy

### Unit Testing (xUnit)

#### Directory Scanner Tests
- [ ] Scans simple directory structure correctly
- [ ] Calculates sizes accurately
- [ ] Handles empty directories
- [ ] Respects depth limits
- [ ] Filters excluded patterns
- [ ] Handles permission errors gracefully
- [ ] Detects and handles circular symlinks

#### ASCII Formatter Tests
- [ ] Generates valid ASCII tree
- [ ] Formats sizes correctly (human-readable)
- [ ] Respects depth limits in output
- [ ] Sorts according to options
- [ ] Handles Unicode filenames
- [ ] Truncates long paths appropriately

#### JSON Serializer Tests
- [ ] Generates valid JSON
- [ ] Compact representation
- [ ] Handles special characters in filenames
- [ ] Correct size aggregation
- [ ] Summary statistics accurate

### Integration Testing

#### End-to-End Scenarios
```bash
# Test ASCII output
dirt.exe
# Expected: Tree displayed in console

# Test treemap with current dir
dirt.exe /t
# Expected: Browser launches with treemap

# Test specific path
dirt.exe C:\test-project
# Expected: Tree of test-project

# Test exclusions
dirt.exe --exclude "*.log,temp/*"
# Expected: No .log files or temp/* in output

# Test depth limit
dirt.exe --depth 2
# Expected: Only 2 levels deep

# Test large directory
dirt.exe C:\Windows /t
# Expected: Handles gracefully, shows progress
```

#### Performance Testing
- [ ] Scan completes within acceptable time
- [ ] Memory usage stays reasonable
- [ ] Single-file binary size < 30MB
- [ ] Startup time < 500ms
- [ ] Treemap renders smoothly in browser

### Manual Testing Checklist

#### CLI Interface
- [ ] No arguments shows current directory ASCII tree
- [ ] `/t` launches browser with treemap
- [ ] Invalid path shows helpful error
- [ ] Exclusion patterns work correctly
- [ ] Depth limiting works correctly
- [ ] Help text (`--help`) is clear

#### Web Server
- [ ] Server starts on specified port
- [ ] Browser launches automatically
- [ ] JSON data endpoint returns correct data
- [ ] Server shuts down cleanly
- [ ] No lingering processes

#### Visual Output
- [ ] Treemap opens in browser
- [ ] Colors are distinguishable
- [ ] Labels are readable
- [ ] Zoom works smoothly
- [ ] Breadcrumbs navigate correctly
- [ ] Tooltips show accurate info
- [ ] Large directories (10k+ files) perform well

## Current Tasks

### Completed
- [x] Project structure setup
- [x] Basic directory traversal
- [x] Treemap HTML generation with D3.js
- [x] File size calculation and aggregation
- [x] Color coding by file extension
- [x] Interactive zoom and navigation
- [x] Breadcrumb UI

### In Progress
- [ ] .NET 8 CLI project setup
- [ ] ASCII tree formatter implementation
- [ ] JSON serializer with temp file handling
- [ ] Kestrel web server integration
- [ ] Browser auto-launch
- [ ] Default exclusion patterns
- [ ] `/t` command alias support
- [ ] Comprehensive PLAN.md documentation

### Backlog

#### High Priority
- [ ] Configuration file support (.dirtrc.json)
- [ ] Default exclusion patterns (bin, obj, node_modules, .git, etc.)
- [ ] Progress indicator for large scans
- [ ] Error handling and user-friendly messages
- [ ] Unit tests for core modules (xUnit)
- [ ] README with examples and usage
- [ ] Native AOT publishing configuration

#### Medium Priority
- [ ] Multiple output formats (JSON to file, CSV, Markdown)
- [ ] Sort options (size, name, type, date)
- [ ] Size threshold filtering
- [ ] File type statistics
- [ ] Comparison mode (diff two scans)
- [ ] Watch mode (monitor changes)
- [ ] Custom color schemes for treemap

#### Low Priority
- [ ] Integration with git (analyze by commit)
- [ ] Export data for other tools
- [ ] Server keep-alive mode
- [ ] Duplicate file detection
- [ ] Compression analysis (identify compressible files)
- [ ] Progress reporting to console during web server run

#### Future Ideas
- [ ] Cost analysis (cloud storage pricing)
- [ ] Historical trend tracking
- [ ] Integration with CI/CD for repo size monitoring
- [ ] Mobile-friendly treemap view
- [ ] Multi-root comparison (compare multiple directories)
- [ ] Plugin system for custom file type handlers

## Performance Considerations

### Current Optimizations
- Async filesystem operations (`Directory.EnumerateFileSystemEntriesAsync`)
- Filtered traversal (skip excluded patterns early)
- In-memory aggregation for fast post-processing
- Efficient data structure (nested objects)
- Native AOT compilation for fast startup

### Future Optimizations
- Parallel directory scanning with `Parallel.ForEachAsync`
- Incremental updates (cache + delta)
- Lazy loading in treemap for huge datasets
- Memoization of frequently scanned paths
- SIMD optimizations for size calculations

### Scalability Targets
- Handle up to 100,000 files in < 10 seconds
- Memory usage < 500MB for typical projects
- Binary size < 30MB (Native AOT)
- Startup time < 500ms
- Treemap interactive at 10,000+ rectangles

## Error Handling

### Expected Errors
- Path not found → "Directory not found: {path}"
- Permission denied → "Access denied: {path} (try running as administrator)"
- Disk I/O errors → "Failed to read: {path}"
- Invalid arguments → "Invalid option: {arg}. Use --help for usage."
- Port already in use → "Port {port} is in use. Try --port {alt}"
- Out of memory → "Directory too large. Try --depth or --exclude."

### Error Response Strategy
- Clear, actionable error messages
- Suggestions for resolution
- Graceful degradation where possible
- Partial results when feasible
- Exit codes for script automation

## Distribution & Installation

### Build Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <PublishAot>true</PublishAot>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <InvariantGlobalization>true</InvariantGlobalization>
  </PropertyGroup>
</Project>
```

### Publishing

```bash
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained

# macOS ARM64
dotnet publish -c Release -r osx-arm64 --self-contained
```

### Requirements
- **Runtime**: None (self-contained)
- **OS**: Windows 10+, Linux (modern distro), macOS 11+
- **Browser**: Any modern browser for treemap viewing

### Package Structure
```
dirt/
├── src/
│   ├── Program.cs           # CLI entry point
│   ├── Scanner.cs           # Directory scanner
│   ├── AsciiFormatter.cs    # ASCII tree output
│   ├── TreemapServer.cs     # Kestrel web server
│   ├── DataSerializer.cs    # JSON serialization
│   └── Models/              # Data models
├── wwwroot/
│   ├── treemap.html         # Treemap UI (embedded)
│   └── (JS/CSS embedded in HTML)
├── test/
│   ├── ScannerTests.cs
│   ├── FormatterTests.cs
│   └── SerializerTests.cs
├── dirt.csproj
├── README.md
└── PLAN.md
```

## Use Cases

### For Developers
- Quick visualization of project structure
- Identify bloated directories
- Find large files before git commit
- Analyze build output sizes
- Understand unfamiliar codebases

### For LLMs
- Parse directory structure as context
- Understand project organization
- Identify relevant files for tasks
- Navigate large codebases efficiently
- Generate architecture documentation

### For DevOps
- Monitor repository growth
- Identify storage optimization opportunities
- Analyze log file accumulation
- Audit deployment artifact sizes
- Capacity planning

## Success Metrics

### Short-term (1 month)
- Core functionality complete and tested
- Binary size < 30MB
- Startup time < 500ms
- Documentation complete
- Working hot reload during development

### Medium-term (3 months)
- Used in 10+ projects
- Community feedback incorporated
- Full test coverage
- CI/CD integration examples
- Performance benchmarks documented

### Long-term (6 months)
- Referenced in .NET tooling articles
- Integration with popular dev tools
- Used by LLM training/context systems
- Multi-platform binaries published
- Community contributions

## Documentation Plan

### User Documentation
- README with quick start
- Command reference (all options)
- Example outputs (screenshots)
- Common use cases and scenarios
- Troubleshooting guide
- Installation instructions

### Developer Documentation
- Architecture overview (this document)
- API documentation (XML docs)
- Contributing guidelines
- Development setup with hot reload
- Testing guide
- Release/publishing process

## Changelog

### v0.1.0 - Initial Development
- .NET 8 CLI application
- ASCII tree output (default)
- Treemap visualization with `/t` flag
- Kestrel web server integration
- Auto-launch browser
- Temp file data contract
- Hot reload development support
- Native AOT compilation

---

*Last Updated: 2025-01-27*
*Project: DirT - Directory Tree Analyzer*
*Purpose: Minimal CLI tool for directory structure analysis and visualization*
*Technology: .NET 8 with Native AOT*

## Implementation Roadmap

### Phase 1: Core CLI & ASCII Output (Milestone 1)
**Goal**: Basic working CLI that outputs ASCII tree

**Tasks**:
1. Create .NET 8 console project with Native AOT support
2. Implement `DirectoryScanner` class
   - Async directory traversal
   - File size calculation
   - Basic error handling
3. Implement `AsciiTreeFormatter` class
   - Box drawing characters (├─, └─, │)
   - Size formatting (KB, MB, GB)
   - Color support (optional)
4. Wire up CLI argument parsing
   - Default: scan current directory
   - Support path argument
5. Write unit tests for scanner and formatter

**Success Criteria**:
- `dirt.exe` outputs ASCII tree of current directory
- `dirt.exe C:\path` outputs tree of specified path
- File sizes displayed correctly
- Tests pass

---

### Phase 2: Data Model & JSON Serialization (Milestone 2)
**Goal**: Structured data representation for web UI

**Tasks**:
1. Define data models (TreeNode, FileSummary)
2. Implement `JsonDataSerializer` class
   - Serialize to `%TEMP%\dirt-data.json`
   - Overwrite existing file
   - Compact JSON (no indentation)
3. Add tests for serialization
4. Document JSON contract in PLAN.md (already done ✓)

**Success Criteria**:
- Valid JSON output
- Temp file created/overwritten correctly
- Contract matches documentation

---

### Phase 3: Web Server & Treemap UI (Milestone 3)
**Goal**: Serve treemap visualization in browser

**Tasks**:
1. Set up minimal Kestrel web server
   - Configure endpoints: `/`, `/data`
   - Serve static HTML (embedded resource)
2. Create `treemap.html` with D3.js
   - Load data from `/data` endpoint
   - Render basic treemap
   - Basic interactivity (click to zoom)
3. Implement browser auto-launch
   - Use `Process.Start("http://localhost:5000")`
   - Cross-platform support
4. Add `/t` command flag
5. Test end-to-end workflow

**Success Criteria**:
- `dirt.exe /t` launches browser with treemap
- Treemap displays directory structure
- Colors and sizes are correct
- Server shuts down cleanly

---

### Phase 4: Filtering & Exclusions (Milestone 4)
**Goal**: Production-ready filtering capabilities

**Tasks**:
1. Implement default exclusion patterns
   - bin/, obj/, node_modules/, .git/, etc.
2. Add `--exclude` option
3. Add `--depth` option
4. Add `--min-size` option
5. Configuration file support (.dirtrc.json)
6. Document all options in README

**Success Criteria**:
- Common dev artifacts excluded by default
- User can override exclusions
- Depth limiting works
- Size filtering works

---

### Phase 5: Polish & Distribution (Milestone 5)
**Goal**: Production-ready release

**Tasks**:
1. Native AOT publishing for win-x64, linux-x64, osx-arm64
2. Size optimization (target < 30MB)
3. Comprehensive error messages
4. Progress indicators for large scans
5. README with examples and screenshots
6. Performance benchmarks
7. Release v1.0.0

**Success Criteria**:
- Binary size < 30MB
- Startup time < 500ms
- Handles 100k+ files
- All tests pass
- Documentation complete

---

## Quick Start for Implementation Agent

### Files to Create (in order)

1. **src/Models/TreeNode.cs**
   - Data model for directory tree
   - Properties: Name, Path, Size, Children, Type, Extension

2. **src/Services/DirectoryScanner.cs**
   - Core scanning logic
   - Method: `Task<TreeNode> ScanAsync(string path, ScanOptions options)`

3. **src/Services/AsciiTreeFormatter.cs**
   - ASCII tree rendering
   - Method: `string Format(TreeNode root, FormatOptions options)`

4. **src/Services/JsonDataSerializer.cs**
   - JSON serialization
   - Method: `Task WriteToTempFileAsync(TreeNode root)`
   - Property: `string TempFilePath { get; }`

5. **src/Services/TreemapServer.cs**
   - Kestrel web server
   - Method: `Task StartAsync(int port)`
   - Method: `void LaunchBrowser(string url)`

6. **src/Program.cs**
   - CLI entry point
   - Argument parsing
   - Command routing

7. **wwwroot/treemap.html**
   - D3.js treemap visualization
   - Embedded in project as resource

8. **test/DirectoryScannerTests.cs**
   - Unit tests for scanner

9. **dirt.csproj**
   - Project configuration
   - Native AOT settings

10. **README.md**
    - Usage instructions
    - Examples

### Key Design Patterns

- **Async/await**: All I/O operations
- **Dependency injection**: WebApplicationBuilder for services
- **Options pattern**: Configuration via IOptions<T>
- **Repository pattern**: Separation of data access
- **Strategy pattern**: Formatter selection (ASCII vs JSON)

### Testing Strategy

- **Unit tests**: xUnit + FluentAssertions
- **Integration tests**: Test full CLI workflows
- **Manual tests**: Checklist in PLAN.md (above)

---

## Questions for Implementation

### Decisions Needed

1. **Box Drawing Characters**: Use Unicode (├─└─│) or ASCII fallback (-|+)?
   - Recommendation: Unicode with ASCII fallback if console doesn't support

2. **Color Support**: Always, never, or auto-detect terminal capability?
   - Recommendation: Auto-detect with `--no-color` override

3. **Server Lifetime**: Auto-shutdown after first request or keep alive?
   - Recommendation: Auto-shutdown with `--keep-server` flag for debugging

4. **Error Verbosity**: Show full stack traces or user-friendly messages?
   - Recommendation: User-friendly by default, `--verbose` for stack traces

5. **Temp File Naming**: Fixed name or random/GUID?
   - Recommendation: Fixed name `dirt-data.json` (simpler, as specified)

### Open Questions

- Should we support stdin input for piped directory listings?
- Should we support output to file instead of console?
- Should progress indicators use spinner, percentage, or both?
- Should we implement a `--watch` mode for continuous monitoring?
