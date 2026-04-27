using Antlr4.Runtime;
using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Statements.Formatters;
using Unipi.Nancy.Playground.MppgParser.Utility;
using Unipi.Nancy.Playground.MppgParser.Visitors;

namespace Unipi.Nancy.Playground.MppgParser;

public record class Program
{
    /// <summary>
    /// The original text of the program.
    /// </summary>
    public string Text { get; init; }

    /// <summary>
    /// The list of statements in the program.
    /// </summary>
    public List<Statement> Statements { get; init; }

    /// <summary>
    /// Errors collected by ANTLR during parsing of this program.
    /// </summary>
    public List<SyntaxErrorInfo> Errors { get; init; }

    /// <summary>
    /// The current program counter.
    /// </summary>
    public int ProgramCounter { get; private set; } = 0;

    /// <summary>
    /// The program execution context.
    /// </summary>
    public ProgramContext ProgramContext { get; init; } =  new ();

    /// <summary>
    /// True if there are no more program statements to execute.
    /// </summary>
    public bool IsEndOfProgram 
        => ProgramCounter >= Statements.Count;

    public Program(List<Statement> statements)
    {
        Statements = statements;
        Text = statements
            .Select(s => s.Text)
            .JoinText("\n");
        Errors = [];
    }

    /// <summary>
    /// Parses the MPPG program from its parse tree and returns the corresponding Program object.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static Program FromTree(Unipi.MppgParser.Grammar.MppgParser.ProgramContext context)
    {
        var visitor = new ProgramVisitor();
        var program = visitor.Visit(context);
        return program with
        {
            Text = context.GetJoinedText()
        };
    }

    /// <summary>
    /// Parses MPPG program text and returns the corresponding Program object.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static Program FromText(string text)
    {
        var errors = new List<SyntaxErrorInfo>();

        var inputStream = CharStreams.fromString(text);
        var lexer = new Unipi.MppgParser.Grammar.MppgLexer(inputStream);
        var lexerListener = new DiagnosticLexerErrorListener(errors, inputStream);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(lexerListener);

        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new Unipi.MppgParser.Grammar.MppgParser(commonTokenStream);
        var parserListener = new DiagnosticParserErrorListener(errors);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(parserListener);

        var context = parser.program();
        var program = FromTree(context);
        return program with
        {
            Text = text,
            Errors = errors
        };
    }

    /// <summary>
    /// Executes the entire program and returns its string output.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> ExecuteToStringOutput()
    {
        while (ProgramCounter < Statements.Count)
        {
            var statementOutput = ExecuteNextStatementToStringOutput();
            foreach (var line in statementOutput)
                yield return line;
        }
    }

    /// <summary>
    /// Executes the next statement in the program and returns its string output.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> ExecuteNextStatementToStringOutput()
    {
        if(IsEndOfProgram)
            yield return $">> end of program";

        var statement = Statements[ProgramCounter++];
        if (statement is not Comment)
            yield return $">> {statement.Text}";
        yield return statement.Execute(ProgramContext.State);
    }

    /// <summary>
    /// Executes the next statement in the program.
    /// </summary>
    /// <param name="formatter"></param>
    /// <param name="immediateComputeValue"></param>
    /// <returns></returns>
    public StatementOutput? ExecuteNextStatement(
        IStatementFormatter formatter,
        bool immediateComputeValue
    )
    {
        if (IsEndOfProgram)
        {
            formatter.FormatEndOfProgram();
            return null;
        }
        else
        {
            var statement = Statements[ProgramCounter++];
            return ProgramContext.ExecuteStatement(
                statement, formatter, immediateComputeValue);
        }
    }

    /// <summary>
    /// Converts the MPPG program to Nancy code.
    /// </summary>
    /// <param name="useNancyExpressions"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public List<string> ToNancyCode(bool useNancyExpressions = false)
    {
        if (Text.IsNullOrWhiteSpace())
            throw new InvalidOperationException("Program text not available!");

        return ToNancyCode(Text,  useNancyExpressions);
    }

    /// <summary>
    /// Converts MPPG program text to Nancy code.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="useNancyExpressions"></param>
    /// <returns></returns>
    public static List<string> ToNancyCode(
        string text, 
        bool useNancyExpressions = false
    )
    {
        var inputStream = CharStreams.fromString(text);
        var lexer = new Unipi.MppgParser.Grammar.MppgLexer(inputStream);
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new Unipi.MppgParser.Grammar.MppgParser(commonTokenStream);

        var programContext = parser.program();
        MppgBaseVisitor<List<string>> visitor = useNancyExpressions 
            ? new ToNancyExpressionsCodeVisitor()
            : new ToNancyCodeVisitor();
        var code = programContext.Accept(visitor);

        return code;
    }
}