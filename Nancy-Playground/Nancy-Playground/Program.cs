using System.Globalization;
using System.Reflection;
using System.Text;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Unipi.Nancy.Playground.Cli;

public class Program
{
    public static string Version => Assembly
        .GetExecutingAssembly()
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(a => a.Key == "PackageVersion")?.Value
        ?? "NA";

    public static string GitCommit => Assembly
            .GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "GitCommit")?.Value
        ?? "NA";

    public static string GitCommitShort => 
        GitCommit.Length >= 7 ? GitCommit[..7] : GitCommit;

    public static string CliVersionLine =>
        $"[green]This is [blue]nancy-playground[/], version {Version} ({GitCommitShort}).[/]";

    public static List<string> CliWelcomeMessage =>
    [
        CliVersionLine,
        // todo: add reference to the maintainer somewhere?
        "[green]Academic attribution: if useful, please cite [yellow]https://doi.org/10.1016/j.softx.2022.101178[/][/]"
    ];

    public static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        if (Console.IsOutputRedirected)
        {
            Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            AnsiConsole.Profile.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            AnsiConsole.Profile.Capabilities.Ansi = false;
            AnsiConsole.Profile.Width = int.MaxValue;
        }

        var app = BuildNancyPlaygroundApp();
        return app.Run(args);
    }

    public static CommandApp<InteractiveCommand> BuildNancyPlaygroundApp()
    {
        var app = new CommandApp<InteractiveCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("nancy-playground");
            config.AddCommand<RunCommand>("run")
                .WithDescription("Runs a .mppg script")
                .WithExample("run", "./Examples/hal-04513292v1.mppg")
                .WithExample("run", "./Examples/hal-04513292v1.mppg", "--output-mode", "MppgClassic", "--run-mode", "PerStatement")
                .WithExample("run", "./Examples/hal-04513292v1.mppg", "--output-mode", "NancyNew", "--run-mode", "ExpressionsBased");

            config.AddCommand<InteractiveCommand>("interactive")
                .WithDescription("Interactive mode, where the user can input MPPG lines one by one.");

            config.AddCommand<ConvertCommand>("convert")
                .WithDescription("Converts a .mppg file to a Nancy program");

#if USE_PLAYWRIGHT
            config.AddCommand<SetupCommand>("setup")
                .WithDescription("Initializes dependencies. Required to enable exporting plots to images.");
#endif

#if DEBUG
            config.PropagateExceptions();
#endif
        });
        return app;
    }
}