using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Statements.Formatters;

namespace Unipi.Nancy.Playground.MppgParser;

public class ProgramContext
{
    public State State { get; init; } =  new ();

    public List<Statement> StatementHistory { get; init; } =  new ();

    public StatementOutput? ExecuteStatement(
        Statement statement,
        IStatementFormatter formatter,
        bool immediateComputeValue
    )
    {
        formatter.FormatStatementPreamble(statement);
        try
        {
            StatementHistory.Add(statement);
            var output = statement switch
            {
                Assignment assignment => assignment.ExecuteToFormattable(State, immediateComputeValue),
                _ => statement.ExecuteToFormattable(State)
            };
            formatter.FormatStatementOutput(statement, output);
            return output;
        }
#pragma warning disable CS0168 // Variable is declared but never used
        catch (Exception e)
#pragma warning restore CS0168 // Variable is declared but never used
        {
            #if DEBUG
            throw;
            #else
            var error = new ErrorOutput
            {
                StatementText = statement.Text,
                OutputText = string.Empty,
                Exception = e
            };
            formatter.FormatError(statement, error);
            return error;
            #endif
        }
    }
}