using Unipi.Nancy.Expressions;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

public class ExpressionOutput : StatementOutput
{
    /// <summary>
    /// The expression that was assigned.
    /// </summary>
    public required IExpression Expression { get; init; }

    /// <summary>
    /// The time it took to compute the results to be compared.
    /// </summary>
    /// <remarks>
    /// May vary depending on whether the expressions were fully computed before comparison or not. 
    /// </remarks>
    public required TimeSpan Time { get; init; }
}