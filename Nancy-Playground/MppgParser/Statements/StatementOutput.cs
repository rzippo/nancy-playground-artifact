namespace Unipi.Nancy.Playground.MppgParser.Statements;

public class StatementOutput
{
    /// <summary>
    /// Text of the statement.
    /// </summary>
    public required string StatementText { get; init; }

    /// <summary>
    /// Output text of the statement.
    /// </summary>
    /// <remarks>
    /// For statements that do not produce simply text, this should be populated as a fallback.
    /// </remarks>
    public required string OutputText { get; init; }
}