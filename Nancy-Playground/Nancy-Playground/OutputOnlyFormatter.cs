using Spectre.Console;
using Unipi.Nancy.Expressions;
using Unipi.Nancy.Playground.Cli.Plots;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Statements.Formatters;
using Unipi.Nancy.Playground.MppgParser.Utility;

namespace Unipi.Nancy.Playground.Cli;

/// <summary>
/// Only prints out the result of explicit print requests.
/// </summary>
public class OutputOnlyFormatter : IStatementFormatter
{
    public IPlotFormatter? PlotFormatter { get; init; }
    public IAnsiConsole Console { get; init; } = AnsiConsole.Console;

    public void FormatStatementPreamble(Statement statement)
    {
        return;
    }

    public void FormatStatementOutput(Statement statement, StatementOutput output)
    {
        switch (statement)
        {
            case ExpressionCommand expression:
            {
                var expressionOutput = (ExpressionOutput)output;
                // assume we are being *required* to compute the expression 
                var expressionValue = expressionOutput.Expression switch
                {
                    CurveExpression ce => ce.Value.ToCodeString(),
                    RationalExpression re => re.Value.ToPrettyString(),
                    _ => throw new InvalidOperationException()
                };
                Console.WriteLine(expressionValue);
                break;
            }
            
            case PlotCommand plot:
            {
                if(PlotFormatter is not null)
                    // we do not control the output of the PlotFormatter
                    PlotFormatter.FormatPlot((PlotOutput) output);
                break;
            }

            case Assertion:
            {
                var assertionOutput = (AssertionOutput)output;
                Console.WriteLine(assertionOutput.Result.ToString().ToLower());
                break;
            }

            // all other outputs are suppressed
            default:
                break;
        }
    }

    public void FormatError(Statement statement, ErrorOutput error)
    {
        return;
    }

    public void FormatEndOfProgram()
    {
        return;
    }
}