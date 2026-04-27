namespace Unipi.Nancy.Playground.MppgParser.Statements;

public class AssignmentOutput : ExpressionOutput
{
    /// <summary>
    /// The variable that was assigned.
    /// </summary>
    public required string AssignedVariable { get; init; }
}