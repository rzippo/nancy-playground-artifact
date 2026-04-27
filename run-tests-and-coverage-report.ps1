#!/bin/pwsh

$ErrorActionPreference = "Stop";

# Cleanup precedent coverage results and  reports
Write-Host "Cleaning up previous test results and coverage reports..." -ForegroundColor Yellow;
Get-ChildItem -Recurse -Filter "TestResults" | Remove-Item -Recurse -Force;
Get-ChildItem -Filter "coveragereport" | Remove-Item -Recurse -Force;

# Run tests with coverage collection
Write-Host "Running new tests with coverage collection..." -ForegroundColor Yellow;
dotnet test --solution ./Nancy-Playground/Nancy-Playground.sln --configuration Release --framework net10.0 --coverlet

# Generate coverage report
Write-Host "Generating coverage report..." -ForegroundColor Yellow;
reportgenerator -reports:"**/net10.0/**/coverage.cobertura*.xml" -targetdir:"coveragereport" -assemblyfilters:"+Unipi.Nancy.Playground*;" -reporttypes:"Html;TextSummary"

Write-Host "Coverage report generated in 'coveragereport' directory. See 'Summary.txt' for a quick overview of the coverage results, or 'index.html' for a detailed report." -ForegroundColor Green;

Write-Host "Here is Summary.txt content:" -ForegroundColor Green;
Get-Content -Path "coveragereport/Summary.txt";