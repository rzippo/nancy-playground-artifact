using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Spectre.Console;
using Spectre.Console.Cli;
using Unipi.Nancy.Playground.Cli.Plots;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Statements.Formatters;

namespace Unipi.Nancy.Playground.Cli;

/// <summary>
/// Specifies where plot output files should be saved.
/// </summary>
public enum PlotRootMode
{
    /// <summary>Save plots in the same directory as the MPPG script file.</summary>
    ScriptDirectory,
    
    /// <summary>Save plots in the current working directory.</summary>
    CurrentDirectory,
    
    /// <summary>Save plots in a manually specified directory.</summary>
    Custom,
}

public class RunCommand : Command<RunCommand.Settings>
{
    private IAnsiConsole Console {get; init;} = AnsiConsole.Console;

    public sealed class Settings : CommonExecutionSettings
    {
        [Description("Path to the .mppg file to run")]
        [CommandArgument(0, "<file>")]
        public string MppgFile { get; init; } = string.Empty;

        [Description("If enabled, makes the output deterministic, removing preamble and time measurements. Useful to implement tests.")]
        [CommandOption("--deterministic")]
        public bool Deterministic { get; init; } = false;

        [Description("Where to save plot output files. Options: ScriptDirectory (default), CurrentDirectory, or Custom. If --plots-root is specified, this defaults to Custom and must not be anything else.")]
        [CommandOption("--plots-root-mode")]
        public PlotRootMode? PlotsRootMode { get; init; }

        [Description("Explicit directory for saving plot files. If specified, --plots-root-mode is assumed to be Custom.")]
        [CommandOption("--plots-root")]
        public string? PlotsRoot { get; init; }

        [Description("If enabled, plots are never opened automatically, regardless of the command used. Useful to implement tests.")]
        [CommandOption("--no-plots-auto-open")]
        public bool NoPlotsAutoOpen { get; init; } = false;
    }

    public RunCommand(IAnsiConsole console)
    {
        Console = console;
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (settings.Version)
        {
            Console.MarkupLine(Program.CliVersionLine);
            return 0;
        }

        if (!settings.MuteWelcomeMessage)
            foreach (var cliWelcomeLine in Program.CliWelcomeMessage)
                Console.MarkupLine(cliWelcomeLine);

        if (string.IsNullOrWhiteSpace(settings.MppgFile))
        {
            Console.MarkupLine($"[red]No input file specified.[/]");
            Console.MarkupLine($"[red]Did you want to run the interactive command?[/]");
            return 1;
        }

        var mppgFile = new FileInfo(settings.MppgFile);
        if (!mppgFile.Exists)
        {
            Console.MarkupLine($"[red]{mppgFile.FullName}: file not found.[/]");
            return 1;
        }

        // in interactive mode, the default is not to echo each command
        var echoInput = settings.EchoInput ?? true;

        // Determine plots root based on the selected mode
        // If --plots-root is specified, --plots-root-mode must be Custom (or omitted, which defaults to Custom)
        string? plotsRoot;
        
        if (!string.IsNullOrWhiteSpace(settings.PlotsRoot))
        {
            // Explicit path provided
            if (settings.PlotsRootMode.HasValue && settings.PlotsRootMode.Value != PlotRootMode.Custom)
            {
                throw new InvalidOperationException("--plots-root is specified with an explicit path, so --plots-root-mode must be Custom or omitted.");
            }
            plotsRoot = Path.GetFullPath(settings.PlotsRoot);
        }
        else
        {
            // Use mode to determine location
            var mode = settings.PlotsRootMode ?? PlotRootMode.ScriptDirectory;
            plotsRoot = mode switch
            {
                PlotRootMode.ScriptDirectory => mppgFile.Directory?.FullName,
                PlotRootMode.CurrentDirectory => Directory.GetCurrentDirectory(),
                PlotRootMode.Custom => throw new InvalidOperationException("--plots-root-mode is Custom but --plots-root was not specified."),
                _ => mppgFile.Directory?.FullName,
            };
        }

        if(!settings.Deterministic)
            Console.MarkupLine($"[yellow]Plots will be saved in: {plotsRoot}[/]");

        var parsingStopwatch = Stopwatch.StartNew();
        var programText = File.ReadAllText(mppgFile.FullName, Encoding.UTF8);
        var program = MppgParser.Program.FromText(programText);
        parsingStopwatch.Stop();

        if (settings.Verbose)
            Console.MarkupLine($"[gray]Parsing completed in {parsingStopwatch.Elapsed.TotalMilliseconds} ms.[/]");

        if (program.Errors.Count > 0)
        {
            if (settings.OnErrorMode == OnErrorMode.Stop)
            {
                Console.MarkupLine("[red]ERROR! Syntax errors, run aborted:[/]");
                foreach(var error in program.Errors)
                    Console.MarkupLineInterpolated($"[red]\t - {error.ToString()}[/]");
                return 1;
            }
            else
            {
                Console.MarkupLine("[darkorange]WARNING! Syntax errors:[/]");
                foreach(var error in program.Errors)
                    Console.MarkupLineInterpolated($"[darkorange]\t - {error.ToString()}[/]");
            }
        }

        var plotFormatter = settings.Deterministic ? null : 
            new ScottPlotFormatter(plotsRoot)
            {
                Console = Console,
                AutoOpenPlots = !settings.NoPlotsAutoOpen
            };
        // add option to use XPlotPlotFormatter?

        IStatementFormatter formatter = settings.OutputMode switch
        {
            OutputMode.ExplicitPrintsOnly => new OutputOnlyFormatter()
            {
                Console = Console,
                PlotFormatter = plotFormatter,
            },
            OutputMode.MppgClassic => new PlainConsoleStatementFormatter(),
            OutputMode.NancyNew => new AnsiConsoleStatementFormatter()
            {
                Console = Console,
                PlotFormatter = plotFormatter,
                PrintTimePerStatement = !settings.Deterministic,
                PrintInputAsConfirmation = false,
                EchoInput = echoInput
            },
            _ => new PlainConsoleStatementFormatter()
        };

        var immediateComputeValue = settings.RunMode switch
        {
            RunMode.ExpressionsBased => false,
            RunMode.PerStatement => true,
            _ => false
        };

        var totalComputationTime = TimeSpan.Zero;
        while (!program.IsEndOfProgram)
        {
            var output = program.ExecuteNextStatement(formatter, immediateComputeValue);
            if(output is ExpressionOutput expressionOutput)
                totalComputationTime += expressionOutput.Time;
            if (settings.OnErrorMode == OnErrorMode.Stop &&
                output is ErrorOutput)
                break;
        }

        // use formatter?
        if(!settings.Deterministic)
            Console.WriteLine($"Total computation time: {totalComputationTime}");
        return 0;
    }
}