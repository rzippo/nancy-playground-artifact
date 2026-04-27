using System.Diagnostics;
using Unipi.Nancy.Expressions;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// Statements without assignement.
/// Most commonly used to print-out the value of a variable.
/// </summary>
public record class ExpressionCommand : Statement
{
    public Expression Expression { get; set; }

    public ExpressionCommand(Expression expression)
    {
        Expression = expression;
    }

    public override string Execute(State state)
    {
        Expression.ParseTree(state);
        var (c, r) = Expression.Compute();

        if (c is not null)
            return c.ToCodeString();
        if (r is not null)
            return r.ToString()!;
        else
            return "undefined";
    }

    public override StatementOutput ExecuteToFormattable(State state)
    {
        var sw = Stopwatch.StartNew();
        Expression.ParseTree(state);
        switch (Expression.NancyExpression)
        {
            case CurveExpression ce:
            {
                ce.Compute();
                break;
            }
            case RationalExpression re:
            {
                re.Compute();
                break;
            }
            default:
                throw new Exception($"Expression could not be parsed");
        }
        sw.Stop();

        var output = Expression.NancyExpression switch
        {
            CurveExpression ce => ce.Value.ToCodeString(),
            RationalExpression re => re.Value.ToCodeString(),
            _ => throw new Exception($"Expression could not be parsed")
        };

        return new ExpressionOutput()
        {
            StatementText = Text,
            OutputText = output,
            Expression = Expression.NancyExpression,
            Time = sw.Elapsed,
        };
    }
}