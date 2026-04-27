using Unipi.Nancy.Expressions;
using Unipi.Nancy.MinPlusAlgebra;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

public class LineParsing
{
    private readonly ITestOutputHelper _testOutputHelper;

    public LineParsing(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static List<(string, List<(string, CurveExpression)>, List<(string, RationalExpression)>)> FunctionExpressions =
    [
        (
            "stair(0, 60, 35)",
            [],
            []
        ),
        (
            "stair (0, 5, 2)",
            [],
            []
        ),
        (
            "stair (0, T4, 12)",
            [],
            [("T4", Expressions.Expressions.FromRational(60, "T4"))]
        ),
        (
            "affine (1 ,0)",
            [],
            []
        ),
        (
            "C + (A1 - C)*zero",
            [
                ("C", Expressions.Expressions.FromCurve(Curve.Zero(), "C")), 
                ("A1", Expressions.Expressions.FromCurve(Curve.Zero(), "A1"))
            ],
            []
        ),
        (
            "C + (A1 + A2 - C)*zero - D1",
            [
                ("C", Expressions.Expressions.FromCurve(Curve.Zero(), "C")), 
                ("A1", Expressions.Expressions.FromCurve(Curve.Zero(), "A1")),
                ("A2", Expressions.Expressions.FromCurve(Curve.Zero(), "A2")),
                ("D1", Expressions.Expressions.FromCurve(Curve.Zero(), "D1"))
            ],
            []
        ),
        (
            "C + (A4 - C)*zero",
            [
                ("C", Expressions.Expressions.FromCurve(Curve.Zero(), "C")), 
                ("A4", Expressions.Expressions.FromCurve(Curve.Zero(), "A4"))
            ],
            []
        ),
        (
            "right-ext(stair(1, 1, 1))",
            [],
            []
        ),
        (
            "( floor comp (D2 / 2) ) * 4",
            [
                ("floor", Expressions.Expressions.FromCurve(Curve.Zero(), "floor")), 
                ("D2", Expressions.Expressions.FromCurve(Curve.Zero(), "D2"))
            ],
            []
        ),
        (
            "C + (A3 + A4 - C)*zero - D4",
            [
                ("C", Expressions.Expressions.FromCurve(Curve.Zero(), "C")), 
                ("A3", Expressions.Expressions.FromCurve(Curve.Zero(), "A3")),
                ("A4", Expressions.Expressions.FromCurve(Curve.Zero(), "A4")),
                ("D4", Expressions.Expressions.FromCurve(Curve.Zero(), "D4"))
            ],
            []
        )
    ];

    public static IEnumerable<object[]> FunctionExpressionsTestCases() =>
        FunctionExpressions.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(FunctionExpressionsTestCases))]
    public void FunctionExpression(
        string expression, 
        List<(string, CurveExpression)> functionVariables, 
        List<(string, RationalExpression)> rationalVariables
    )
    {
        var state = new State(functionVariables, rationalVariables);

        var ie = ExpressionParsing.Parse(expression, state);

        switch (ie)
        {
            case CurveExpression ce:
                _testOutputHelper.WriteLine(ce.ToUnicodeString(showRationalsAsName: true));
                break;
            case RationalExpression re:
                _testOutputHelper.WriteLine(re.ToUnicodeString(showRationalsAsName: true));
                break;
            default:
                _testOutputHelper.WriteLine("No output.");
                break;
        }
    }
}