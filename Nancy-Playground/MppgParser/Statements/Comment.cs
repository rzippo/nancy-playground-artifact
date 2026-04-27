namespace Unipi.Nancy.Playground.MppgParser.Statements;

public record class Comment : Statement
{
    public override string Execute(State state)
    {
        // todo: make optional?
        return Text;
    }

    public override StatementOutput ExecuteToFormattable(State state)
    {
        return new StatementOutput
        {
            StatementText = Text,
            OutputText = Text
        };
    }
}