namespace Unipi.Nancy.Playground.MppgParser.Statements;

public class ErrorOutput : StatementOutput
{
    /// <summary>
    /// The exception emitted for the error.
    /// </summary>
    public required Exception Exception { get; init; }
}