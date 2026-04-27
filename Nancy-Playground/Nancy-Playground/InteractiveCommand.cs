using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;
using Unipi.Nancy.Playground.Cli.Plots;
using Unipi.Nancy.Playground.MppgParser;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Statements.Formatters;

namespace Unipi.Nancy.Playground.Cli;

[ExcludeFromCodeCoverage]
public partial class InteractiveCommand : Command<InteractiveCommand.Settings>
{
    public sealed class Settings : CommonExecutionSettings
    {

    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (settings.Version)
        {
            AnsiConsole.MarkupLine(Program.CliVersionLine);
            return 0;
        }

        if (!settings.MuteWelcomeMessage)
            foreach (var cliWelcomeLine in Program.CliWelcomeMessage)
                AnsiConsole.MarkupLine(cliWelcomeLine);

        var programContext = new ProgramContext();

        // in interactive mode, the default is not to echo each command
        var echoInput = settings.EchoInput ?? false;

        // todo: make this configurable
        var plotsRoot = Environment.CurrentDirectory;

        IStatementFormatter formatter = settings.OutputMode switch
        {
            OutputMode.MppgClassic => new PlainConsoleStatementFormatter(),
            OutputMode.NancyNew => new AnsiConsoleStatementFormatter()
            {
                // todo: make this configurable
                PlotFormatter = new ScottPlotFormatter(plotsRoot),
                // PlotFormatter = new XPlotPlotFormatter(plotsRoot),
                PrintInputAsConfirmation = true,
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

        var lineEditor = new LineEditor(Keywords, ContextualKeywords());
        var totalComputationTime = TimeSpan.Zero;

        // CLI welcome message
        AnsiConsole.MarkupLine("[green]Interactive mode: type in your commands. Use [blue]!help[/] to read the manual.[/]");

        while (true)
        {
            var line = lineEditor.ReadLine().Trim();
            if (string.IsNullOrWhiteSpace(line))
                AnsiConsole.WriteLine();
            else if (line.StartsWith("!"))
            {
                // interactive mode command
                if (line.StartsWith("!quit") || line.StartsWith("!exit"))
                {
                    AnsiConsole.MarkupLine("[green]Bye.[/]");
                    break;
                }
                else if (line.StartsWith("!export") || line.StartsWith("!save"))
                {
                    var args = line.Split(' ').Skip(1).ToArray();
                    ExportProgram(args, programContext);
                }
                else if (line.StartsWith("!convert"))
                {
                    var args = line.Split(' ').Skip(1).ToArray();
                    ConvertProgram(args, programContext);
                }
                else if (line.StartsWith("!load"))
                {
                    var args = line.Split(' ').Skip(1).ToArray();
                    LoadProgram(args, programContext, formatter, immediateComputeValue, lineEditor);
                }
                else if (line.StartsWith("!clear"))
                {
                    var args = line.Split(' ').Skip(1).ToArray();
                    bool clearHistory = args.Any(arg => arg == "-h" || arg == "--history");
                    
                    programContext = new ProgramContext();
                    lineEditor.SetSessionKeywords([]);
                    
                    if (clearHistory)
                    {
                        lineEditor.ClearHistory();
                        AnsiConsole.MarkupLine("[green]Session cleared. All variables, executed lines, and command history have been reset.[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[green]Session cleared. All variables and executed lines have been reset.[/]");
                    }
                }
                else if (line.StartsWith("!help"))
                {
                    var args = line.Split(' ').Skip(1).ToArray();
                    PrintHelp(args);
                }
                else if (line.StartsWith("!clihelp"))
                {
                    var app = Program.BuildNancyPlaygroundApp();
                    app.Run(["--help"]);
                }
            }
            else
            {
                // MPPG syntax statement
                var statement = Statement.FromLine(line);
                programContext.ExecuteStatement(statement, formatter, immediateComputeValue);

                // update session-based autocomplete
                // could be optimized to be diff-based
                lineEditor.SetSessionKeywords(programContext.State.GetVariableNames());
            }
        }

        return 0;
    }

    /// <summary>
    /// Exports the current program to a file.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="programContext"></param>
    private void ExportProgram(string[] args, ProgramContext programContext)
    {
        if (args.Length != 1)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] !export requires exactly one argument: the output file path.");
            return;
        }

        var outputPath = args[0];
        try
        {
            var statementLines = Enumerable.Select(programContext.StatementHistory, s => s.Text);

            File.WriteAllLines(outputPath, statementLines);
            AnsiConsole.MarkupLine($"[green]Program exported successfully to[/] [blue]{Escape(outputPath)}[/].");
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Could not export program to [blue]{Escape(outputPath)}[/]: {Escape(e.Message)}");
        }
    }

    /// <summary>
    /// Converts the current MPPG program to Nancy code and writes it to a file.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="programContext"></param>
    private void ConvertProgram(string[] args, ProgramContext programContext)
    {
        if (args.Length != 1)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] !convert requires exactly one argument: the output file path.");
            return;
        }

        var outputPath = args[0];
        try
        {
            var statementLines = Enumerable.Select(programContext.StatementHistory, s => s.Text);
            var programText = string.Join(Environment.NewLine, statementLines);
            var programNancyCode = Unipi.Nancy.Playground.MppgParser.Program.ToNancyCode(programText);
            programNancyCode.InsertRange(0,[
                $"// Program automatically converted from MPPG syntax to a Nancy program",
                string.Empty,
                $"// This is a file-based app: to run it, use the command `dotnet run file.cs`",
                $"// To extend it, it is recommended to convert it to a C# project with the command `dotnet project convert file.cs`",
                $"// Docs: https://learn.microsoft.com/en-us/dotnet/core/sdk/file-based-apps",
                string.Empty
            ]);

            File.WriteAllLines(outputPath, (IEnumerable<string>)programNancyCode);
            AnsiConsole.MarkupLine($"[green]Program converted successfully to[/] [blue]{Escape(outputPath)}[/].");
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Could not export program to [blue]{Escape(outputPath)}[/]: {Escape(e.Message)}");
        }
    }

    /// <summary>
    /// Loads and executes a program from a file.
    /// </summary>
    /// <param name="args">Arguments containing the file path and optional flags</param>
    /// <param name="programContext">The current program context</param>
    /// <param name="formatter">The statement formatter to use</param>
    /// <param name="immediateComputeValue">Whether to compute values immediately</param>
    /// <param name="lineEditor">The line editor for updating keywords</param>
    private void LoadProgram(string[] args, ProgramContext programContext, IStatementFormatter formatter, bool immediateComputeValue, LineEditor lineEditor)
    {
        if (args.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] !load requires at least one argument: the input file path.");
            return;
        }

        // Parse options
        bool addToHistory = false;
        string filePath = string.Empty;

        foreach (var arg in args)
        {
            if (arg == "-h" || arg == "--history")
            {
                addToHistory = true;
            }
            else if (!arg.StartsWith("-"))
            {
                filePath = arg;
            }
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] !load requires a file path argument.");
            return;
        }

        try
        {
            var file = new FileInfo(filePath);
            if (!file.Exists)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] File [blue]{Escape(filePath)}[/] not found.");
                return;
            }

            // Create a formatter with EchoInput enabled, preserving all other settings from the provided formatter
            IStatementFormatter loadFormatter = formatter switch
            {
                AnsiConsoleStatementFormatter ansi => new AnsiConsoleStatementFormatter()
                {
                    PlotFormatter = ansi.PlotFormatter,
                    PrintInputAsConfirmation = ansi.PrintInputAsConfirmation,
                    PrintTimePerStatement = ansi.PrintTimePerStatement,
                    EchoInput = true
                },
                _ => formatter
            };

            var lines = File.ReadAllLines(filePath);
            int successCount = 0;
            int errorCount = 0;
            var loadedLines = new List<string>();

            AnsiConsole.MarkupLine($"[green]Loading program from[/] [blue]{Escape(filePath)}[/]...");

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("//"))
                    continue;

                // Track the line for history if requested
                if (addToHistory)
                {
                    loadedLines.Add(trimmedLine);
                }

                // Execute the statement
                try
                {
                    var statement = Statement.FromLine(trimmedLine);
                    programContext.ExecuteStatement(statement, loadFormatter, immediateComputeValue);
                    successCount++;
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error executing line:[/] {Escape(trimmedLine)}");
                    AnsiConsole.MarkupLine($"[red]{Escape(ex.Message)}[/]");
                    errorCount++;
                }
            }
            // Update session-based autocomplete with new variables
            lineEditor.SetSessionKeywords(programContext.State.GetVariableNames());

            // Add loaded lines to history if requested
            if (addToHistory && loadedLines.Count > 0)
            {
                lineEditor.AddToHistory(loadedLines);
                AnsiConsole.MarkupLine($"[green]Program loaded:[/] {successCount} statements executed, {loadedLines.Count} lines added to history");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]Program loaded:[/] {successCount} statements executed");
            }

            if (errorCount > 0)
                AnsiConsole.MarkupLine($"[yellow]Warnings:[/] {errorCount} errors encountered");
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Could not load program from [blue]{Escape(filePath)}[/]: {Escape(e.Message)}");
        }
    }
}