#!/bin/pwsh
param(
    [Parameter(Mandatory=$true)]
    [string]$MppgScriptPath,

    [Parameter(Mandatory=$false)]
    [int]$Iterations = 5,

    [switch]$OutputLatex = $false
)

$ErrorActionPreference = "Stop"

$benchmarkDate = Get-Date;

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Join-Path $scriptDir "Nancy-Playground"
$workingDir = Join-Path $scriptDir "benchmark-temp"
$convertedDir = Join-Path $workingDir "converted"

$MppgScriptPath = [System.IO.Path]::GetFullPath($MppgScriptPath)

New-Item -ItemType Directory -Path $workingDir -Force | Out-Null
New-Item -ItemType Directory -Path $convertedDir -Force | Out-Null

Write-Host "Working directory: $workingDir" -ForegroundColor Cyan

$cliProject = Join-Path $projectDir "Nancy-Playground\Nancy-Playground.csproj"
$mappingProject = Join-Path $projectDir "MppgParser\MppgParser.csproj"

$cliDllPath = Join-Path $projectDir "Nancy-Playground\bin\Release\net10.0\Unipi.Nancy.Playground.Cli.dll"
$mappingDllPath = Join-Path $projectDir "MppgParser\bin\Release\net10.0\Unipi.Nancy.Playground.MppgParser.dll"

Write-Host "`n=== Step 1: Building Nancy-Playground net10.0 Release ===" -ForegroundColor Yellow
dotnet build $cliProject -c Release -f net10.0 --nologo -v q | Out-Null
if ($LASTEXITCODE -ne 0) { throw "CLI build failed" }
Write-Host "CLI build complete" -ForegroundColor Green

function Measure-Benchmark {
    param(
        [string]$ExePath,
        [string]$WorkingDir,
        [string[]]$Arguments,
        [int]$Iterations = 5,
        [int]$MemorySamplingInterval = 10
    )

    $times = @()
    $memories = @()

    $isVerbose = $VerbosePreference -eq "Continue";
    
    $dotnetArguments = "$ExePath $($Arguments -join ' ')"
    for ($i = 0; $i -lt $Iterations; $i++) {
        Write-Verbose "Iteration $($i + 1) of $Iterations`: $dotnetArguments"
        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName = "dotnet"
        $psi.Arguments = $dotnetArguments
        $psi.WorkingDirectory = $WorkingDir
        $psi.UseShellExecute = $false
        if($isVerbose){
            $psi.RedirectStandardOutput = $false
            $psi.RedirectStandardError = $false
        }
        else {
            $psi.RedirectStandardOutput = $true
            $psi.RedirectStandardError = $true
        }

        $proc = [System.Diagnostics.Process]::Start($psi)
        if(-not $isVerbose) {
            $stdoutTask = $proc.StandardOutput.ReadToEndAsync()
            $stderrTask = $proc.StandardError.ReadToEndAsync()
        }
        
        [int64]$maxMemory = 0
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        do {
            $maxMemory = [math]::Max([int64]$maxMemory, [int64]$proc.PeakWorkingSet64)
            $exited = $proc.WaitForExit($MemorySamplingInterval)
        } while (!$exited -and !$proc.HasExited)
        $stopwatch.Stop()

        $times += $stopwatch.ElapsedMilliseconds
        $memories += $maxMemory

        Start-Sleep -Milliseconds 100
    }

    $avgTime = ($times | Measure-Object -Average).Average
    $avgMem = ($memories | Measure-Object -Average).Average / 1MB

    return @{
        AvgTimeMs = [math]::Round($avgTime, 2)
        AvgMemoryMB = [math]::Round($avgMem, 2)
    }
}

$benchmarks = @{
    "Nancy-Playground run" = @{ TimeMs = 0; MemoryMB = 0 }
    "Nancy-Playground convert" = @{ TimeMs = 0; MemoryMB = 0 }
    "Converted C#" = @{ TimeMs = 0; MemoryMB = 0 }
}

Write-Host "`n=== Step 2: Benchmarking Nancy-Playground run ===" -ForegroundColor Yellow

$runResult = Measure-Benchmark -ExePath $cliDllPath -WorkingDir $workingDir -Arguments @("run", $MppgScriptPath) -Iterations $Iterations

Write-Host "  Avg Time: $($runResult.AvgTimeMs) ms" -ForegroundColor Cyan
Write-Host "  Avg Memory: $($runResult.AvgMemoryMB) MB" -ForegroundColor Cyan

$benchmarks["Nancy-Playground run"].TimeMs = $runResult.AvgTimeMs
$benchmarks["Nancy-Playground run"].MemoryMB = $runResult.AvgMemoryMB

Write-Host "`n=== Step 3: Benchmarking Nancy-Playground convert ===" -ForegroundColor Yellow

$outputCsPath = Join-Path $convertedDir "program.cs"
$convertArgs = @("convert", $MppgScriptPath, "--output-file", $outputCsPath)

$convertResult = Measure-Benchmark -ExePath $cliDllPath -WorkingDir $convertedDir -Arguments $convertArgs -Iterations $Iterations

Write-Host "  Avg Time: $($convertResult.AvgTimeMs) ms" -ForegroundColor Cyan
Write-Host "  Avg Memory: $($convertResult.AvgMemoryMB) MB" -ForegroundColor Cyan

$benchmarks["Nancy-Playground convert"].TimeMs = $convertResult.AvgTimeMs
$benchmarks["Nancy-Playground convert"].MemoryMB = $convertResult.AvgMemoryMB

if (-not (Test-Path $outputCsPath)) {
    throw "Convert failed - output file not created"
}

Write-Host "Converted to: $outputCsPath" -ForegroundColor Green

$csprojContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>14</LangVersion>
    <Features>FileBasedProgram</Features>
    <NoWarn>CS9298</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$cliProject" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="$mappingProject" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Unipi.Nancy.Expressions" Version="1.0.1" />
  </ItemGroup>
</Project>
"@

$generatedCsproj = Join-Path $convertedDir "ConvertedProject.csproj"
$csprojContent | Set-Content $generatedCsproj

$convertProjectDir = Join-Path $convertedDir "ConvertedProject"
New-Item -ItemType Directory -Path $convertProjectDir -Force | Out-Null
Move-Item $outputCsPath $convertProjectDir\program.cs -Force
Move-Item $generatedCsproj $convertProjectDir\ConvertedProject.csproj -Force

Write-Host "`n=== Step 4: Building Converted Program ===" -ForegroundColor Yellow

$buildOutput = dotnet build $convertProjectDir\ConvertedProject.csproj -c Release -f net10.0 --nologo -v q -p:NoWarn=CS9298 2>&1
if ($LASTEXITCODE -ne 0) { throw "Converted program build failed: $buildOutput" }
Write-Host "Converted program build complete" -ForegroundColor Green

$convertedDll = Join-Path $convertProjectDir "bin\Release\net10.0\ConvertedProject.dll"

Write-Host "`n=== Step 5: Benchmarking Converted Program ===" -ForegroundColor Yellow

$convResult = Measure-Benchmark -ExePath $convertedDll -WorkingDir $convertProjectDir -Arguments @() -Iterations $Iterations

Write-Host "  Avg Time: $($convResult.AvgTimeMs) ms" -ForegroundColor Cyan
Write-Host "  Avg Memory: $($convResult.AvgMemoryMB) MB" -ForegroundColor Cyan

$benchmarks["Converted C#"].TimeMs = $convResult.AvgTimeMs
$benchmarks["Converted C#"].MemoryMB = $convResult.AvgMemoryMB

Write-Host "`n=== Comparison Table ===" -ForegroundColor Yellow

$asciiTable = @"
+--------------------------------+-------------------+--------------------+
|                                | Runtime (ms)      | Peak Memory (MB)   |
+--------------------------------+-------------------+--------------------+
| Nancy-Playground run           | $($benchmarks["Nancy-Playground run"].TimeMs.ToString().PadLeft(17)) | $($benchmarks["Nancy-Playground run"].MemoryMB.ToString().PadLeft(18)) |
| Nancy-Playground convert       | $($benchmarks["Nancy-Playground convert"].TimeMs.ToString().PadLeft(17)) | $($benchmarks["Nancy-Playground convert"].MemoryMB.ToString().PadLeft(18)) |
| Converted C#                   | $($benchmarks["Converted C#"].TimeMs.ToString().PadLeft(17)) | $($benchmarks["Converted C#"].MemoryMB.ToString().PadLeft(18)) |
+--------------------------------+-------------------+--------------------+
"@
Write-Host $asciiTable

if($OutputLatex){
    $latexTable = @"
\begin{table}[h]
    \centering
    \begin{tabular}{|l|r|r|}
    \hline
     & Runtime (ms) & Peak Memory (MB) \\
    \hline
    Nancy-Playground run & $($benchmarks["Nancy-Playground run"].TimeMs) & $($benchmarks["Nancy-Playground run"].MemoryMB) \\
    \hline
    Nancy-Playground convert & $($benchmarks["Nancy-Playground convert"].TimeMs) & $($benchmarks["Nancy-Playground convert"].MemoryMB) \\
    \hline
    Converted C\# & $($benchmarks["Converted C#"].TimeMs) & $($benchmarks["Converted C#"].MemoryMB) \\
    \hline
    \end{tabular}
\end{table}
"@
    Write-Host $latexTable
}

$summaryFile = Join-Path $scriptDir "benchmark-results-$([System.IO.Path]::GetFileNameWithoutExtension($MppgScriptPath))-$($benchmarkDate.ToString("yyyyMMdd-HH-mm-ss")).txt"

$summary = "Benchmark Results
===============
Script: $MppgScriptPath
Date: $($benchmarkDate.ToString("yyyy-MM-dd HH:mm:ss"))

$asciiTable"
$summary | Set-Content $summaryFile
Write-Host "`nResults saved to: $summaryFile" -ForegroundColor Gray

Remove-Item -Path $workingDir -Recurse -Force

Write-Host "`nBenchmark complete!" -ForegroundColor Green