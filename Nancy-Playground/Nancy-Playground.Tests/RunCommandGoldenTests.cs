using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using CliWrap;
using CliWrap.Buffered;
using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;

namespace Unipi.Nancy.Playground.Cli.Tests;

using CliMarker = Cli.Program;
public class RunCommandGoldenTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public RunCommandGoldenTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static IEnumerable<string> GoldenTestDirs()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "run-testcases");
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Missing testcases folder: {root}");

        var caseDirs = Directory
            .EnumerateDirectories(root, "*", SearchOption.AllDirectories)
            .Where(IsCaseDirectory)
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase);

        return caseDirs;
    }

    private static bool IsCaseDirectory(string dir) 
        => File.Exists(Path.Combine(dir, "expected.exitcode.txt"));

    public static IEnumerable<object[]> GoldenTestCases() 
        => GoldenTestDirs().Select(dir => (object[])[dir]);

    private static IReadOnlyList<string> ReadArgs(string path)
    {
        if (!File.Exists(path)) return Array.Empty<string>();

        return File.ReadAllLines(path)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Where(l => !l.StartsWith("#"))
            .ToArray();
    }

    private static string ReadFileOrEmpty(string path) =>
        File.Exists(path) ? File.ReadAllText(path) : "";

    private static string ReadRequired(string path) =>
        File.Exists(path) ? File.ReadAllText(path)
            : throw new FileNotFoundException($"Missing required testcase file: {path}");

    private static string Normalize(string s) =>
        s.Replace("\r\n", "\n").Trim();

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
    /// Builds and launches the actual app, testing its CLI output. 
    /// </summary>
    [Theory]
    [MemberData(nameof(GoldenTestCases))]
    [ExcludeFromCodeCoverage]
    public async Task CliTestEquivalence(string caseDir)
    {
        // Arrange: locate the CLI dll built for *this* test run's TFM.
        // Because this test project is multi-targeted, dotnet test runs it per TFM.
        var cliDllPath = typeof(CliMarker).Assembly.Location;

        _testOutputHelper.WriteLine($"cliDllPath: {cliDllPath}");
        _testOutputHelper.WriteLine($"caseDir: {Path.GetFullPath(caseDir)}");

        if (string.IsNullOrWhiteSpace(cliDllPath) || !File.Exists(cliDllPath))
            throw new FileNotFoundException($"CLI assembly not found at: {cliDllPath}");

        var tfm = GetCurrentTfmFromPath(cliDllPath);

        List<string> args = [
            "run",
            Path.Combine(caseDir, "input.txt"),
            "--deterministic",
            "--no-welcome"
        ];
        args.AddRange(ReadArgs(Path.Combine(caseDir, "args.txt")));

        var expectedOut = Normalize(ReadFileOrEmpty(Path.Combine(caseDir, "expected.stdout.txt")));
        var expectedErr = Normalize(ReadFileOrEmpty(Path.Combine(caseDir, "expected.stderr.txt")));
        var expectedExit = int.Parse(ReadRequired(Path.Combine(caseDir, "expected.exitcode.txt")).Trim());

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        BufferedCommandResult result;
        try
        {
            // Run framework-dependent output as: dotnet <YourCli.dll> <args...>
            var dotnetArgs = new List<string> { cliDllPath };
            dotnetArgs.AddRange(args);

            result = await CliWrap.Cli.Wrap("dotnet")
                .WithArguments(dotnetArgs)
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8, cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"CLI did not exit within 30 seconds (TFM={tfm}, case={caseDir}).");
        }

        var actualOut = Normalize(result.StandardOutput);
        var actualErr = Normalize(result.StandardError);

        // Assert + dump actuals on mismatch (include TFM to avoid clobber across runs)
        try
        {
            Assert.Equal(expectedExit, result.ExitCode);
            Assert.Equal(expectedOut, actualOut);
            Assert.Equal(expectedErr, actualErr);
        }
        catch
        {
            File.WriteAllText(Path.Combine(caseDir, $"actual.{tfm}.stdout.txt"), result.StandardOutput);
            File.WriteAllText(Path.Combine(caseDir, $"actual.{tfm}.stderr.txt"), result.StandardError);
            File.WriteAllText(Path.Combine(caseDir, $"actual.{tfm}.exitcode.txt"), result.ExitCode.ToString());
            throw;
        }
    }
    
    /// <summary>
    /// Tests the run command using an AppTester.
    /// </summary>
    /// <remarks>
    /// Provides a debug path, and test coverage metrics.
    /// </remarks>
    [Theory]
    [MemberData(nameof(GoldenTestCases))]
    public void AppTesterEquivalence(string caseDir)
    {
        // must be setup here, since AppTesters inherit it
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        // Arrange
        _testOutputHelper.WriteLine($"caseDir: {Path.GetFullPath(caseDir)}");

        List<string> args = [
            "run",
            Path.Combine(caseDir, "input.txt"),
            "--deterministic",
            "--no-welcome"
        ];
        args.AddRange(ReadArgs(Path.Combine(caseDir, "args.txt")));

        var expectedOut = Normalize(ReadFileOrEmpty(Path.Combine(caseDir, "expected.stdout.txt")));
        var expectedErr = Normalize(ReadFileOrEmpty(Path.Combine(caseDir, "expected.stderr.txt")));
        var expectedExit = int.Parse(ReadRequired(Path.Combine(caseDir, "expected.exitcode.txt")).Trim());

        var console = new TestConsole();
        console.Profile.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        console.Profile.Capabilities.Ansi = false;
        console.Profile.Width = int.MaxValue;
        
        var app = new CommandAppTester(console: console);
        app.Configure(config =>
        {
            config.AddCommand<RunCommand>("run");
        });

        // Act
        var result = app.Run(args.ToArray());
        
        var actualOut = Normalize(console.Output);
        
        // Assert
        Assert.Equal(expectedExit, result.ExitCode);
        Assert.Equal(expectedOut, actualOut);
        // no current way to test stderr in Spectre.Console?
    }
}