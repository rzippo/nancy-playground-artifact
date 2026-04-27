namespace Unipi.Nancy.Playground.MppgParser.Statements.Formatters;

public interface IStatementFormatter
{
    /// <summary>
    /// Formats output before statement execution.
    /// Useful to output something before long commands.
    /// </summary>
    /// <param name="statement"></param>
    public void FormatStatementPreamble(Statement statement);
  
    /// <summary>
    /// Formats output after successful execution.
    /// </summary>
    /// <param name="statement"></param>
    /// <param name="output"></param>
    public void FormatStatementOutput(Statement statement, StatementOutput output);

    /// <summary>
    /// Formats output of an error.
    /// </summary>
    /// <param name="statement"></param>
    /// <param name="error"></param>
    public void FormatError(Statement statement, ErrorOutput error);

    /// <summary>
    /// Formats output for end of program reached.
    /// </summary>
    public void FormatEndOfProgram();
}