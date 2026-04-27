using Antlr4.Runtime;
using Unipi.Nancy.Playground.MppgParser.Visitors;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

public abstract record class Statement
{
    public string Text { get; init; } = string.Empty;

    public string InlineComment { get; init; } = string.Empty;

    public abstract string Execute(State state);

    public abstract StatementOutput ExecuteToFormattable(State state);

    public static Statement FromLine(string line)
    {
        var inputStream = CharStreams.fromString(line);
        var lexer = new Unipi.MppgParser.Grammar.MppgLexer(inputStream);
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new Unipi.MppgParser.Grammar.MppgParser(commonTokenStream);

        var context = parser.statement();
        var visitor = new StatementVisitor();
        var statement = visitor.Visit(context);
        return statement;
    }
}