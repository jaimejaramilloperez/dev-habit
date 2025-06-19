#!/bin/bash
#
# coverage.sh - Run .NET tests with code coverage and generate an HTML report.
#
# Usage:
#   ./scripts/coverage.sh [output_dir] [verbosity]
#
# Arguments:
#   output_dir   Directory for the coverage report (default: coverage)
#   verbosity    ReportGenerator verbosity (default: Error)
#
# Requirements:
#   - dotnet CLI
#   - reportgenerator (must be in PATH)
#
# Example:
#   ./scripts/coverage.sh my-coverage Info

set -euo pipefail

# --- Check for required tools ---
if ! command -v dotnet &> /dev/null
then
  echo "Error: dotnet CLI is not installed or not in PATH." >&2
  exit 1
fi
if ! command -v reportgenerator &> /dev/null
then
  echo "Error: reportgenerator is not installed or not in PATH." >&2
  echo "Install with: dotnet tool install -g dotnet-reportgenerator-globaltool" >&2
  exit 1
fi

# --- Parse arguments ---
OUTPUT_DIR="${1:-coverage}"
VERBOSITY="${2:-Error}"

echo "Cleaning previous results..."
find . \( -name "TestResults" -o -name "$OUTPUT_DIR" \) -type d -exec rm -rf {} +

echo "Running all tests with coverage..."
dotnet test --verbosity quiet --collect:"XPlat Code Coverage"

echo "Generating report..."
reportgenerator \
  -reports:"test/**/TestResults/**/coverage.cobertura.xml" \
  -targetdir:"$OUTPUT_DIR" \
  -reporttypes:"Html;HtmlSummary" \
  -filefilters:"-**/obj/**;-**/*.g.cs;-**/bin/**;-**/Migrations/**;-**/*Designer.cs" \
  -verbosity:"$VERBOSITY"

echo "Report generated at: $OUTPUT_DIR/index.html"
