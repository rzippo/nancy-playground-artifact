using Spectre.Console;
using Unipi.Nancy.Expressions;
using Unipi.Nancy.Playground.Cli.Plots;
using Unipi.Nancy.Playground.Cli.Utility;
using Unipi.Nancy.Playground.MppgParser.Exceptions;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Statements.Formatters;
using Unipi.Nancy.Playground.MppgParser.Utility;

namespace Unipi.Nancy.Playground.Cli;

public class AnsiConsoleStatementFormatter : IStatementFormatter
{
    public IPlotFormatter? PlotFormatter { get; init; }

    /// <summary>
    /// If true, the statement text is printed in gray, as confirmation to a prompt above (e.g., in interactive mode).
    /// If false, it is instead printed in $mainColor (e.g., in run mode). 
    /// </summary>
    public bool PrintInputAsConfirmation { get; init; } = false;

    /// <summary>
    /// If true, echoes user input in interactive mode.
    /// If false, input is only echoed on syntax errors.
    /// </summary>
    public bool EchoInput { get; init; } = false;

    /// <summary>
    /// If true, prints the time taken to execute each statement.
    /// </summary>
    public bool PrintTimePerStatement { get; init; } = true;

    /// <summary>
    /// The console used for output.
    /// Defaults to a stdout console.
    /// </summary>
    public IAnsiConsole Console { get; init; } = AnsiConsole.Console;

    public void FormatStatementPreamble(Statement statement)
    {
        switch (statement)
        {
            case Comment comment:
            {
                // do nothing
                break;
            }

            case EmptyStatement es:
            {
                // do nothing
                break;
            }

            default:
            {
                if(EchoInput)
                {
                    if(PrintInputAsConfirmation)
                    {
                        // use gray text, to not attract focus
                        if (statement.InlineComment.IsNullOrWhiteSpace())
                            Console.MarkupLineInterpolated($"[grey]» {statement.Text}[/]");
                        else
                            Console.MarkupLineInterpolated($"[grey]» {statement.Text}[/] [green]{statement.InlineComment}[/]");
                    }
                    else
                    {
                        // use $mainColor text
                        if (statement.InlineComment.IsNullOrWhiteSpace())
                            Console.MarkupLineInterpolated($"> {statement.Text}");
                        else
                            Console.MarkupLineInterpolated($"> {statement.Text} [green]{statement.InlineComment}[/]");
                    }
                }
                break;
            }
        }
    }

    public void FormatStatementOutput(Statement statement, StatementOutput output)
    {
        switch (statement)
        {
            case ExpressionCommand expression:
            {
                var expressionOutput = (ExpressionOutput) output;
                var formattedTime = FormatStatementTime(expressionOutput.Time);
                if (expressionOutput.Expression.IsComputed)
                {
                    var expressionValue = expressionOutput.Expression switch
                    {
                        CurveExpression ce => ce.Value.ToCodeString(),
                        RationalExpression re => re.Value.ToPrettyString(),
                        _ => throw new InvalidOperationException()
                    };
                    Console.MarkupLineInterpolated(formattedTime.Concat($"[magenta]{expressionValue}[/]"));
                }
                else
                {
                    Console.MarkupLineInterpolated(formattedTime.Concat($"[magenta]{expressionOutput.OutputText}[/]"));
                }
                break;
            }

            case Assignment assignment:
            {
                var assignmentOutput = (AssignmentOutput) output;
                var formattedTime = FormatStatementTime(assignmentOutput.Time);
                if (assignmentOutput.Expression.IsComputed)
                {
                    var expressionValue = assignmentOutput.Expression switch
                    {
                        CurveExpression ce => ce.Value.ToCodeString(),
                        RationalExpression re => re.Value.ToPrettyString(),
                        _ => throw new InvalidOperationException()
                    };
                    Console.MarkupLineInterpolated(formattedTime.Concat($"{assignmentOutput.AssignedVariable} = [magenta]{expressionValue}[/]"));
                }
                else
                {
                    Console.MarkupLineInterpolated(formattedTime.Concat($"{assignmentOutput.AssignedVariable} = [magenta]{assignmentOutput.Expression.ToUnicodeString()}[/]"));
                }
                break;
            }

            case Assertion assertion:
            {
                var assertionOutput = (AssertionOutput) output;
                Console.MarkupLineInterpolated(FormatStatementTime(assertionOutput.Time).Concat($"[magenta]{output.OutputText}[/]"));
                break;
            }

            case Comment comment:
            {
                Console.MarkupLineInterpolated($"[green]{comment.Text}[/]");
                break;
            }

            case EmptyStatement es:
            {
                // do nothing
                break;
            }

            case PlotCommand plot:
            {
                if(PlotFormatter is not null)
                {
                    var plotOutput = (PlotOutput) output;
                    if (plotOutput.Time > TimeSpan.Zero && PrintTimePerStatement)
                    {
                        Console.MarkupLineInterpolated(FormatStatementTime(plotOutput.Time).Concat($"[grey]Plot inputs computed.[/]"));
                    }
                    PlotFormatter.FormatPlot(plotOutput);
                }
                else
                    Console.MarkupLineInterpolated($"[yellow]Plots disabled.[/]");
                break;
            }

            default:
            {
                Console.MarkupLineInterpolated($"{output.OutputText}");
                break;
            }
        }
    }

    /// <summary>
    /// If <see cref="PrintTimePerStatement"/> is true, formats the given timespan with markup.
    /// As it returns a FormattableString, the interpolation is not resolved.
    ///
    /// If <see cref="PrintTimePerStatement"/> is false, it returns an empty string intead.
    /// </summary>
    private FormattableString FormatStatementTime(TimeSpan time)
    {
        if (PrintTimePerStatement)
            return $"[blue][[{time}]][/] ";
        else
            return $"";
    }

    public void FormatError(Statement statement, ErrorOutput error)
    {
        // On syntax errors, echo the input if it hasn't been echoed yet
        if (error.Exception is SyntaxErrorException && !EchoInput && !PrintInputAsConfirmation)
        {
            // Echo the input that caused the syntax error
            if (statement is not Comment and not EmptyStatement)
            {
                if (statement.InlineComment.IsNullOrWhiteSpace())
                    Console.MarkupLineInterpolated($"[grey]» {statement.Text}[/]");
                else
                    Console.MarkupLineInterpolated($"[grey]» {statement.Text}[/] [green]{statement.InlineComment}[/]");
            }
        }

        switch(error.Exception)
        {
            case SyntaxErrorException:
            {
                Console.MarkupLineInterpolated($"[red]Syntax error[/]: {error.Exception.Message}");
                break;
            }

            default:
            {
                Console.MarkupLineInterpolated($"[red]Execution error[/]: {error.Exception.Message}");
                break;
            }
        }

    }

    public void FormatEndOfProgram()
    {
        Console.MarkupLineInterpolated($"[yellow]End of Program.[/]");
    }
}