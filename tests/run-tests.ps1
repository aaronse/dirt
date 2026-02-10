#!/usr/bin/env pwsh
# Integration tests for DirT
# Usage: 
#   ./run-tests.ps1           Run tests and compare against expected outputs
#   ./run-tests.ps1 -Update   Regenerate all expected output files

param(
    [switch]$Update = $false
)

$ErrorActionPreference = "Stop"
$Root = Split-Path $PSScriptRoot -Parent
$DirtExe = Join-Path $Root "bin\Debug\net8.0\dirt.exe"
$FixturesDir = Join-Path $PSScriptRoot "fixtures"
$ExpectedDir = Join-Path $PSScriptRoot "expected"

# Build if needed
if (-not (Test-Path $DirtExe)) {
    Write-Host "Building DirT..." -ForegroundColor Yellow
    Push-Location $Root
    dotnet build -c Debug | Out-Null
    Pop-Location
}

# Test definitions: name, fixture, args
$Tests = @(
    @{
        Name = "01-baseline-no-flags"
        Desc = "Directory with only excluded files should NOT appear without flags"
        Fixture = "empty-after-filter"
        Args = @("/x:*.mp4")
    },
    @{
        Name = "02-ah-flag"
        Desc = "Directory with only excluded files SHOULD appear with /ah"
        Fixture = "empty-after-filter"
        Args = @("/ah", "/x:*.mp4")
    },
    @{
        Name = "03-ah-c-flags"
        Desc = "Directory should show counts with /ah /c"
        Fixture = "empty-after-filter"
        Args = @("/ah", "/x:*.mp4", "/c")
    },
    @{
        Name = "04-show-ignored-flag"
        Desc = "Directory should appear with /show-ignored"
        Fixture = "empty-after-filter"
        Args = @("/show-ignored", "/x:*.mp4")
    },
    @{
        Name = "05-show-ignored-c-flags"
        Desc = "Directory should show counts with /show-ignored /c"
        Fixture = "empty-after-filter"
        Args = @("/show-ignored", "/x:*.mp4", "/c")
    },
    @{
        Name = "06-mixed-content"
        Desc = "Directory with both excluded and visible files should always appear"
        Fixture = "mixed-content"
        Args = @("/x:*.mp4;*.json")
    },
    @{
        Name = "07-nested-empty-no-ah"
        Desc = "Nested empty-after-filter directory should NOT appear without /ah"
        Fixture = "nested-empty"
        Args = @("/x:*.mp4")
    },
    @{
        Name = "08-nested-empty-with-ah"
        Desc = "Nested empty-after-filter directory SHOULD appear with /ah"
        Fixture = "nested-empty"
        Args = @("/ah", "/x:*.mp4")
    }
)

$Passed = 0
$Failed = 0
$FailedTests = @()

foreach ($Test in $Tests) {
    $FixturePath = Join-Path $FixturesDir $Test.Fixture
    $ExpectedFile = Join-Path $ExpectedDir "$($Test.Name).txt"
    
    # Run dirt and capture output
    $Output = & $DirtExe $FixturePath $Test.Args 2>&1 | Out-String
    
    if ($Update) {
        # Update mode: save output as expected
        $Output | Set-Content -Path $ExpectedFile -NoNewline -Encoding UTF8
        Write-Host "[UPDATE] $($Test.Name)" -ForegroundColor Cyan
        Write-Host "  $($Test.Desc)" -ForegroundColor DarkGray
    } else {
        # Test mode: compare against expected
        if (-not (Test-Path $ExpectedFile)) {
            Write-Host "[FAIL] $($Test.Name)" -ForegroundColor Red
            Write-Host "  Expected file not found: $ExpectedFile" -ForegroundColor Red
            Write-Host "  Run with -Update to generate expected outputs" -ForegroundColor Yellow
            $Failed++
            $FailedTests += $Test.Name
            continue
        }
        
        $Expected = Get-Content -Path $ExpectedFile -Raw -Encoding UTF8
        
        if ($Output -eq $Expected) {
            Write-Host "[PASS] $($Test.Name)" -ForegroundColor Green
            Write-Host "  $($Test.Desc)" -ForegroundColor DarkGray
            $Passed++
        } else {
            Write-Host "[FAIL] $($Test.Name)" -ForegroundColor Red
            Write-Host "  $($Test.Desc)" -ForegroundColor DarkGray
            Write-Host "  Expected:" -ForegroundColor Yellow
            Write-Host $Expected -ForegroundColor DarkYellow
            Write-Host "  Actual:" -ForegroundColor Yellow
            Write-Host $Output -ForegroundColor DarkYellow
            $Failed++
            $FailedTests += $Test.Name
        }
    }
}

Write-Host ""
if ($Update) {
    Write-Host "Updated $($Tests.Count) expected output files" -ForegroundColor Cyan
} else {
    Write-Host "Results: $Passed passed, $Failed failed" -ForegroundColor $(if ($Failed -eq 0) { "Green" } else { "Red" })
    
    if ($Failed -gt 0) {
        Write-Host ""
        Write-Host "Failed tests:" -ForegroundColor Red
        foreach ($FailedTest in $FailedTests) {
            Write-Host "  - $FailedTest" -ForegroundColor Red
        }
        exit 1
    }
}

exit 0
