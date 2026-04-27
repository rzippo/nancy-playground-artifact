using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CliWrap;
using CliWrap.Buffered;
using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;

namespace Unipi.Nancy.Playground.Cli.Tests;

using CliMarker = Cli.Program;

/// <summary>
/// Tests that plot commands produce the same PNG images when running
/// the MPPG script and when running the converted C# program.
/// </summary>
public class ConvertCommandPlotTests
{
    #pragma warning disable xUnit1051 // recommends xUnit cancellation token

    private readonly ITestOutputHelper _testOutputHelper;

    public ConvertCommandPlotTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static IEnumerable<string> TestDirs()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "plot-testcases");
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Missing testcases folder: {root}");

        var caseDirs = Directory
            .EnumerateDirectories(root, "*", SearchOption.AllDirectories)
            .Where(IsCaseDirectory)
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase);

        return caseDirs;
    }

    public static IEnumerable<object[]> TestCases() 
        => TestDirs().Select(dir => (object[])[dir]);

    private static bool IsCaseDirectory(string dir) =>
        File.Exists(Path.Combine(dir, "script.mppg"));

    private static string GetCurrentTfmFromPath(string assemblyPath)
    {
        // Typical path contains .../bin/Release/<tfm>/...
        // We'll grab the first segment that looks like netX.Y
        var parts = assemblyPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        var tfm = parts.FirstOrDefault(p => p.StartsWith("net", StringComparison.OrdinalIgnoreCase));
        return tfm ?? "unknown-tfm";
    }

    /// <summary>
    /// Computes SHA256 hash of a file for comparison.
    /// This allows us to compare images deterministically.
    /// </summary>
    private static async Task<string> ComputeFileHashAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        using (var sha256 = SHA256.Create())
        using (var stream = File.OpenRead(filePath))
        {
            var hash = await sha256.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    /// <summary>
    /// Extracts all plot output file paths from stdout.
    /// Plot commands print the full path to the generated image file.
    /// </summary>
    private static IEnumerable<string> ExtractPlotPathsFromStdout(string stdout)
    {
        return stdout
            .Replace("\r\n", "\n")
            .Split('\n')
            .Where(line => !string.IsNullOrWhiteSpace(line) && line.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .Select(line => line.Trim());
    }

    /// <summary>
    /// Extracts all plot output file paths from a directory.
    /// </summary>
    private static IEnumerable<string> ExtractPlotPaths(string dir)
    {
        return Directory
            .EnumerateFiles(dir, "*.png", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Builds and launches the actual app: first in run mode, then convert.
    /// Then builds and launches the converted script to test that the same plots are produced. 
    /// </summary>
    [Theory]
    [MemberData(nameof(TestCases))]
    [ExcludeFromCodeCoverage]
    public async Task CliSamePlotImages(string caseDir)
    {
        // Arrange: locate the CLI dll built for *this* test run's TFM.
        var cliDllPath = typeof(CliMarker).Assembly.Location;

        _testOutputHelper.WriteLine($"cliDllPath: {cliDllPath}");
        _testOutputHelper.WriteLine($"caseDir: {Path.GetFullPath(caseDir)}");

        if (string.IsNullOrWhiteSpace(cliDllPath) || !File.Exists(cliDllPath))
            throw new FileNotFoundException($"CLI assembly not found at: {cliDllPath}");

        var tfm = GetCurrentTfmFromPath(cliDllPath);

        var outputDir = Path.Combine(caseDir, "plot-comparison-test", "cli");
        Directory.CreateDirectory(outputDir);

        // Create subdirectories for run and convert outputs to avoid conflicts
        var runOutputDir = Path.Combine(outputDir, "run");
        var convertOutputDir = Path.Combine(outputDir, "convert");
        Directory.CreateDirectory(runOutputDir);
        Directory.CreateDirectory(convertOutputDir);
        _testOutputHelper.WriteLine($"runOutputDir: {Path.GetFullPath(runOutputDir)}");
        _testOutputHelper.WriteLine($"convertOutputDir: {Path.GetFullPath(convertOutputDir)}");

        var scriptPath = Path.Combine(caseDir, "script.mppg");
        List<string> runCommandArgs = [
            "run",
            scriptPath,
            "--no-welcome",
            "--plots-root", runOutputDir,
            "--no-plots-auto-open"
        ];
        
        // Act: Run the MPPG script to generate plots
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
        {
            BufferedCommandResult runCommandResult;
            try
            {
                var dotnetRunCommandArgs = new List<string> { cliDllPath };
                dotnetRunCommandArgs.AddRange(runCommandArgs);

                runCommandResult = await CliWrap.Cli.Wrap("dotnet")
                    .WithArguments(dotnetRunCommandArgs)
                    .WithWorkingDirectory(runOutputDir)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Run command did not exit within 30 seconds (TFM={tfm}, case={caseDir}).");
            }

            await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.stdout.txt"), runCommandResult.StandardOutput, cts.Token);
            await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.stderr.txt"), runCommandResult.StandardError, cts.Token);
            await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.exitcode.txt"), runCommandResult.ExitCode.ToString(), cts.Token);

            Assert.Equal(0, runCommandResult.ExitCode);
        }

        // Arrange: convert the MPPG script to a C# file-based app
        var programPath = Path.Combine(convertOutputDir, "program.cs");
        List<string> convertCommandArgs = [
            "convert", 
            scriptPath, 
            "--output-file", programPath, 
            "--overwrite" 
        ];

        // Act: convert command, obtain the C# program
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
        {
            BufferedCommandResult convertCommandResult;
            try
            {
                // Run framework-dependent output as: dotnet <YourCli.dll> <args...>
                var dotnetConvertCommandArgs = new List<string> { cliDllPath };
                dotnetConvertCommandArgs.AddRange(convertCommandArgs);

                convertCommandResult = await CliWrap.Cli.Wrap("dotnet")
                    .WithArguments(dotnetConvertCommandArgs)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Convert command did not exit within 30 seconds (TFM={tfm}, case={caseDir}).");
            }

            await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.stdout.txt"), convertCommandResult.StandardOutput, cts.Token);
            await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.stderr.txt"), convertCommandResult.StandardError, cts.Token);
            await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.exitcode.txt"), convertCommandResult.ExitCode.ToString(), cts.Token);

            Assert.True(File.Exists(programPath), $"Converted program not found at {programPath}");
            Assert.Equal(0, convertCommandResult.ExitCode);
        }

        // Act: Run the converted C# program to generate plots
        string convertPlotPaths;
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
        {
            BufferedCommandResult programResult;
            try
            {
                var dotnetProgramArgs = new List<string> { programPath };

                programResult = await CliWrap.Cli.Wrap("dotnet")
                    .WithArguments(dotnetProgramArgs)
                    .WithWorkingDirectory(convertOutputDir)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Program did not exit within 30 seconds (TFM={tfm}, case={caseDir}).");
            }

            Assert.Equal(0, programResult.ExitCode);
            convertPlotPaths = programResult.StandardOutput;
        }

        // Assert: Verify that plot files exist and have matching content
        var runPlotFiles = ExtractPlotPaths(runOutputDir).ToList();
        var convertPlotFiles = ExtractPlotPaths(convertOutputDir).ToList();

        _testOutputHelper.WriteLine($"Run plot files: [ {string.Join(", ", runPlotFiles)} ]");
        _testOutputHelper.WriteLine($"Convert plot files: [ {string.Join(", ", convertPlotFiles)} ]");

        // Both runs should produce the same number of plot files
        Assert.Equal(runPlotFiles.Count, convertPlotFiles.Count);

        // Compare each plot file by hash
        for (int i = 0; i < runPlotFiles.Count; i++)
        {
            var runPlotFile = runPlotFiles[i];
            var convertPlotFile = convertPlotFiles[i];

            // Extract just the filename for comparison (paths may differ)
            var runFileName = Path.GetFileName(runPlotFile);
            var convertFileName = Path.GetFileName(convertPlotFile);

            Assert.Equal(runFileName, convertFileName);

            // Get full paths
            var runFilePath = Path.Combine(runOutputDir, runFileName);
            var convertFilePath = Path.Combine(convertOutputDir, convertFileName);

            Assert.True(File.Exists(runFilePath), $"Run plot file not found: {runFilePath}");
            Assert.True(File.Exists(convertFilePath), $"Convert plot file not found: {convertFilePath}");

            // Compare file hashes
            var runHash = await ComputeFileHashAsync(runFilePath);
            var convertHash = await ComputeFileHashAsync(convertFilePath);

            _testOutputHelper.WriteLine($"Plot file: {runFileName}");
            _testOutputHelper.WriteLine($"  Run hash:     {runHash}");
            _testOutputHelper.WriteLine($"  Convert hash: {convertHash}");

            Assert.Equal(runHash, convertHash);
        }
    }

    /// <summary>
    /// Tests the run and convert commands via AppTesters.
    /// Then builds and launches the converted script to test that the same plots are produced. 
    /// </summary>
    /// <remarks>
    /// Provides a debug path, and test coverage metrics.
    /// </remarks>
    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task AppTesterSamePlotImages(string caseDir)
    {
        // must be setup here, since AppTesters inherit it
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        // Arrange
        // we locate the CLI dll only for the TFM string
        var cliDllPath = typeof(CliMarker).Assembly.Location;
        var tfm = GetCurrentTfmFromPath(cliDllPath);

        _testOutputHelper.WriteLine($"caseDir: {Path.GetFullPath(caseDir)}");

        var outputDir = Path.Combine(caseDir, "plot-comparison-test", "app-tester");
        Directory.CreateDirectory(outputDir);

        // Create subdirectories for run and convert outputs to avoid conflicts
        var runOutputDir = Path.Combine(outputDir, "run");
        var convertOutputDir = Path.Combine(outputDir, "convert");
        Directory.CreateDirectory(runOutputDir);
        Directory.CreateDirectory(convertOutputDir);
        _testOutputHelper.WriteLine($"runOutputDir: {Path.GetFullPath(runOutputDir)}");
        _testOutputHelper.WriteLine($"convertOutputDir: {Path.GetFullPath(convertOutputDir)}");

        var scriptPath = Path.Combine(caseDir, "script.mppg");
        List<string> runCommandArgs = [
            "run",
            scriptPath,
            "--no-welcome",
            "--plots-root", runOutputDir,
            "--no-plots-auto-open"
        ];
        
        var runConsole = new TestConsole();
        runConsole.Profile.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        runConsole.Profile.Capabilities.Ansi = false;
        runConsole.Profile.Width = int.MaxValue;
        
        var runApp = new CommandAppTester(console: runConsole);
        runApp.Configure(config =>
        {
            config.AddCommand<RunCommand>("run");
        });

        // Act: Run the MPPG script to generate plots
        var runCommandResult = runApp.Run(runCommandArgs.ToArray());
        
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.stdout.txt"), runCommandResult.Output);
        // await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.stderr.txt"), runCommandResult.StandardError);
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.exitcode.txt"), runCommandResult.ExitCode.ToString());
        
        Assert.Equal(0, runCommandResult.ExitCode);

        // Arrange: convert the MPPG script to a C# file-based app
        var programPath = Path.Combine(convertOutputDir, "program.cs");
        List<string> convertCommandArgs = [
            "convert", 
            scriptPath, 
            "--output-file", programPath, 
            "--overwrite" 
        ];

        var convertConsole = new TestConsole();
        convertConsole.Profile.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        convertConsole.Profile.Capabilities.Ansi = false;
        convertConsole.Profile.Width = int.MaxValue;
        
        var convertApp = new CommandAppTester(console: convertConsole);
        convertApp.Configure(config =>
        {
            config.AddCommand<ConvertCommand>("convert");
        });

        // Act: convert command, obtain the C# program
        var convertCommandResult = convertApp.Run(convertCommandArgs.ToArray());
        
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.stdout.txt"), convertCommandResult.Output);
        // await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.stderr.txt"), convertCommandResult.StandardError);
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.exitcode.txt"), convertCommandResult.ExitCode.ToString());

        Assert.True(File.Exists(programPath));
        Assert.Equal(0, convertCommandResult.ExitCode);

        // Act: Run the converted C# program to generate plots (from here on, identical to the CLI test)
        string convertPlotPaths;
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
        {
            BufferedCommandResult programResult;
            try
            {
                var dotnetProgramArgs = new List<string> { programPath };

                programResult = await CliWrap.Cli.Wrap("dotnet")
                    .WithArguments(dotnetProgramArgs)
                    .WithWorkingDirectory(convertOutputDir)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Program did not exit within 30 seconds (TFM={tfm}, case={caseDir}).");
            }

            Assert.Equal(0, programResult.ExitCode);
            convertPlotPaths = programResult.StandardOutput;
        }

        // Assert: Verify that plot files exist and have matching content
        var runPlotFiles = ExtractPlotPaths(runOutputDir).ToList();
        var convertPlotFiles = ExtractPlotPaths(convertOutputDir).ToList();

        _testOutputHelper.WriteLine($"Run plot files: [ {string.Join(", ", runPlotFiles)} ]");
        _testOutputHelper.WriteLine($"Convert plot files: [ {string.Join(", ", convertPlotFiles)} ]");

        // Both runs should produce the same number of plot files
        Assert.Equal(runPlotFiles.Count, convertPlotFiles.Count);

        // Compare each plot file by hash
        for (int i = 0; i < runPlotFiles.Count; i++)
        {
            var runPlotFile = runPlotFiles[i];
            var convertPlotFile = convertPlotFiles[i];

            // Extract just the filename for comparison (paths may differ)
            var runFileName = Path.GetFileName(runPlotFile);
            var convertFileName = Path.GetFileName(convertPlotFile);

            Assert.Equal(runFileName, convertFileName);

            // Get full paths
            var runFilePath = Path.Combine(runOutputDir, runFileName);
            var convertFilePath = Path.Combine(convertOutputDir, convertFileName);

            Assert.True(File.Exists(runFilePath), $"Run plot file not found: {runFilePath}");
            Assert.True(File.Exists(convertFilePath), $"Convert plot file not found: {convertFilePath}");

            // Compare file hashes
            var runHash = await ComputeFileHashAsync(runFilePath);
            var convertHash = await ComputeFileHashAsync(convertFilePath);

            _testOutputHelper.WriteLine($"Plot file: {runFileName}");
            _testOutputHelper.WriteLine($"  Run hash:     {runHash}");
            _testOutputHelper.WriteLine($"  Convert hash: {convertHash}");

            Assert.Equal(runHash, convertHash);
        }
    }
}
