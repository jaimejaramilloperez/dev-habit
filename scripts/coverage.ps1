<#
.SYNOPSIS
  Run .NET tests with code coverage and generate an HTML report.

.DESCRIPTION
  This script runs `dotnet test` with coverage and uses `reportgenerator` to generate an HTML report.

.PARAMETER OutputDir
  Directory for the coverage report (default: coverage)

.PARAMETER Verbosity
  ReportGenerator verbosity (default: Error)

.EXAMPLE
  ./scripts/coverage.ps1 -OutputDir my-coverage -Verbosity Info
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$OutputDir = "coverage",

    [Parameter(Position = 1)]
    [string]$Verbosity = "Error"
)

# Set strict error handling
$ErrorActionPreference = "Stop"

# --- Check for required tools ---
Write-Host "Checking for required tools..."

try {
    $null = Get-Command dotnet -ErrorAction Stop
} catch {
    Write-Error "Error: dotnet CLI is not installed or not in PATH."
    exit 1
}

try {
    $null = Get-Command reportgenerator -ErrorAction Stop
} catch {
    Write-Error "Error: reportgenerator is not installed or not in PATH."
    Write-Error "Install with: dotnet tool install -g dotnet-reportgenerator-globaltool"
    exit 1
}

Write-Host "Cleaning previous results..."

# Remove TestResults directories
Get-ChildItem -Path . -Recurse -Directory -Name "TestResults" -ErrorAction SilentlyContinue |
    ForEach-Object { Remove-Item -Path $_ -Recurse -Force }

# Remove output directory if it exists
if (Test-Path $OutputDir) {
    Remove-Item -Path $OutputDir -Recurse -Force
}

Write-Host "Running all tests with coverage..."
dotnet test --verbosity quiet --collect:"XPlat Code Coverage"

Write-Host "Generating report..."

# Find coverage files
$coverageFiles = Get-ChildItem -Path "test" -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue |
    ForEach-Object { $_.FullName }

if (-not $coverageFiles) {
    Write-Error "No coverage files found. Make sure tests ran successfully."
    exit 1
}

$reports = $coverageFiles -join ";"

reportgenerator `
    -reports:"$reports" `
    -targetdir:"$OutputDir" `
    -reporttypes:"Html;HtmlSummary" `
    -filefilters:"-**/obj/**;-**/*.g.cs;-**/bin/**;-**/Migrations/**;-**/*Designer.cs" `
    -verbosity:"$Verbosity"

Write-Host "Report generated at: $OutputDir\index.html"
