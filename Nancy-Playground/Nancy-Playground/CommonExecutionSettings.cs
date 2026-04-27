using System.ComponentModel;
using Spectre.Console.Cli;

namespace Unipi.Nancy.Playground.Cli;

public class CommonExecutionSettings : CommandSettings
{
    [CommandOption("-o|--output-mode")] 
    [Description("How the output is formatted. Available options: ExplicitPrintsOnly, MppgClassic, NancyNew (default).")]
    public OutputMode? OutputMode { get; init; } 
        = Cli.OutputMode.NancyNew;
    
    [CommandOption("-r|--run-mode")]
    [Description("How the computations are performed. Available options are PerStatement (computes the result of each line as it comes), ExpressionsBased (computes only as needed, e.g. for plots and value prints). Default: ExpressionsBased.")]
    public RunMode? RunMode { get; init; }
        = Cli.RunMode.ExpressionsBased;
    
    [CommandOption("-e|--on-error")]
    [Description("Specifies what to do when an error occurs. Available options: Stop (default), Continue.")]
    public OnErrorMode? OnErrorMode { get; init; }
        = Cli.OnErrorMode.Stop;

    [CommandOption("--no-welcome")]
    [Description("Mutes the welcome message.")]
    public bool MuteWelcomeMessage { get; init; } = false;

    [CommandOption("--echo")]
    [Description("Echoes user input in interactive mode. Default: true in run mode, false in interactive mode.")]
    public bool? EchoInput { get; init; }

    [CommandOption("--verbose")]
    [Description("If enabled, the program prints out additional information about the execution, such as the time taken during parsing. Default: false.")]
    public bool Verbose {get; init;} = false;

    [Description("If used, the program prints out the version and immediately terminates.")]
    [CommandOption("--version")]
    public bool Version { get; init; } = false;
}

public enum OutputMode
{
    /// <summary>
    /// Only prints when explicitly asked with a non-assignment expression.
    /// </summary>
    ExplicitPrintsOnly,
    /// <summary>
    /// Follows the output style of RTaW Min-Plus Playground.
    /// </summary>
    MppgClassic,
    /// <summary>
    /// Uses a richer custom output style.
    /// </summary>
    NancyNew
}

public enum RunMode
{
    /// <summary>
    /// Each statement trigger its related computation.
    /// </summary>
    PerStatement,
    /// <summary>
    /// Statements build up expressions, which are lazily evaluated only when required.
    /// </summary>
    ExpressionsBased
}

public enum OnErrorMode
{
    /// <summary>
    /// On error, the execution stops.
    /// </summary>
    Stop,
    /// <summary>
    /// On error, continue to the next statement.
    /// This is what RTaW Min-Plus Playground does.
    /// </summary>
    Continue
}