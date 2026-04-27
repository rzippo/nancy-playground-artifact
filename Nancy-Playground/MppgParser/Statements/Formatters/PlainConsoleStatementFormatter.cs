namespace Unipi.Nancy.Playground.MppgParser.Statements.Formatters;

public class PlainConsoleStatementFormatter: IStatementFormatter
{
    public void FormatStatementPreamble(Statement statement)
    {
        if(statement is not Comment)
            Console.WriteLine(statement.Text);
    }

    public void FormatStatementOutput(Statement statement, StatementOutput output)
    {
        Console.WriteLine($">> {output.OutputText}");
    }

    public void FormatError(Statement statement, ErrorOutput error)
    {
        Console.WriteLine($"Error: {error.Exception.Message}");
    }

    public void FormatEndOfProgram()
    {
        Console.WriteLine(">> end of program");
    }
}