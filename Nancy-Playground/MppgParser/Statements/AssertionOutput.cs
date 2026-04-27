using Unipi.Nancy.Expressions;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

public class AssertionOutput : StatementOutput
{
    /// <summary>
    /// The left expression.
    /// </summary>
    public required IExpression LeftExpression { get; init; }

    /// <summary>
    /// The right expression.
    /// </summary>
    public required IExpression RightExpression { get; init; }

    /// <summary>
    /// The time it took to compute the results to be compared.
    /// </summary>
    /// <remarks>
    /// May vary depending on whether the expressions were fully computed before comparison or not. 
    /// </remarks>
    public required TimeSpan Time { get; init; }

    /// <summary>
    /// The result of the comparison.
    /// </summary>
    public required bool Result { get; init; }
}