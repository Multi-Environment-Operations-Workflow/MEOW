#!/bin/bash

cd MEOW_TESTING

# Run tests and capture output
output=$(dotnet test --collect:"XPlat Code Coverage")

# Extract the GUID from the output (looking for TestResults/<guid> pattern)
guid=$(echo "$output" | grep -o "TestResults/[a-f0-9-]*" | head -1 | cut -d'/' -f2)

# Check if GUID was found
if [ -z "$guid" ]; then
    echo "Error: Could not find test results GUID"
    exit 1
fi

echo "Found GUID: $guid"

# Generate report
reportgenerator -reports:"TestResults/$guid/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html

#
xdg-open coveragereport/index.html
