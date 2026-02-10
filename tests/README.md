# DirT Integration Tests

Snapshot-based integration tests for command-line behavior.

## Running Tests

```powershell
# Run all tests
.\run-tests.ps1

# Update expected outputs (after intentional behavior changes)
.\run-tests.ps1 -Update
```

## Test Structure

- **`fixtures/`** - Test directory structures with known content
- **`expected/`** - Expected stdout for each test case
- **`run-tests.ps1`** - Test runner script

## Test Coverage

| Test | Scenario | Validates |
|------|----------|-----------|
| 01-baseline-no-flags | Dir with only excluded files, no flags | Backward compatibility - dir should NOT appear |
| 02-ah-flag | Dir with only excluded files + `/ah` | ShowAll makes empty-after-filtering dirs visible |
| 03-ah-c-flags | Dir with only excluded files + `/ah /c` | Counts displayed beneath directory |
| 04-show-ignored-flag | Dir with only excluded files + `/show-ignored` | Secondary trigger works |
| 05-show-ignored-c-flags | Dir with only excluded files + `/show-ignored /c` | Both flags work together |
| 06-mixed-content | Dir with both excluded and visible files | Normal dirs always appear |
| 07-nested-empty-no-ah | Nested dir with only excluded files, no `/ah` | Nested empty dirs hidden by default |
| 08-nested-empty-with-ah | Nested dir with only excluded files + `/ah` | Nested empty dirs visible with `/ah` |

## Adding New Tests

1. Create fixture directory structure in `fixtures/`
2. Add test definition to `$Tests` array in `run-tests.ps1`
3. Run `.\run-tests.ps1 -Update` to generate expected output
4. Verify the expected output is correct
5. Run `.\run-tests.ps1` to confirm test passes

## Notes

- Tests execute in ~1-2 seconds total
- Expected outputs are plain text for easy git diffing
- Cross-platform compatible (PowerShell Core)
