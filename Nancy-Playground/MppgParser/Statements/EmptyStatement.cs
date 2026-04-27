namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// Empty statement are supported for compatibility, but should be effectively ignored.
/// </summary>
public record class EmptyStatement : Statement
{
    public override string Execute(State state)
    {
        return string.Empty;
    }

    public override StatementOutput ExecuteToFormattable(State state)
    {
        return new StatementOutput
        {
            StatementText = string.Empty,
            OutputText = string.Empty
        };
    }
}