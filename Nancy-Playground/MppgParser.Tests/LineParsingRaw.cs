using Antlr4.Runtime;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

public class LineParsingRaw
{
    private readonly ITestOutputHelper _testOutputHelper;

    public LineParsingRaw(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static List<string> FunctionExpressions =
    [
        "stair(0, 60, 35)",
        "stair (0, 5, 2)",
        "stair (0, T4, 12)",
        "affine (1 ,0)",
        "C + (A1 - C)*zero",
        "C + (A1 + A2 - C)*zero - D1",
        "C + (A4 - C)*zero",
        "right-ext(stair(1, 1, 1))",
        "( floor comp (D2 / 2) ) * 4",
        "C + (A3 + A4 - C)*zero - D4"
    ];

    public static IEnumerable<object[]> FunctionExpressionsTestCases() =>
        FunctionExpressions.ToXUnitTestCases();
    
    [Theory]
    [MemberData(nameof(FunctionExpressionsTestCases))]
    public void FunctionExpression(string expression)
    {
        var inputStream = CharStreams.fromString(expression);
        var lexer = new Unipi.MppgParser.Grammar.MppgLexer(inputStream);
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new Unipi.MppgParser.Grammar.MppgParser(commonTokenStream);

        var context = parser.functionExpression();
        
        // Process the parse tree
        _testOutputHelper.WriteLine(context.ToStringTree(parser));
    }
}