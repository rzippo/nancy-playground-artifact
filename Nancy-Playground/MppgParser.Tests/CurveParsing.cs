using Unipi.Nancy.Expressions;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.NetworkCalculus;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

public class CurveParsing
{
    private readonly ITestOutputHelper _testOutputHelper;

    public CurveParsing(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static List<(string mppg, Curve expected)> KnownMppgCurvePairs =
    [
        (
            "ratency(1, 2)",
            new RateLatencyServiceCurve(1, 2)
        ),
        (
            "ratency(1, 0)",
            new RateLatencyServiceCurve(1, 0)
        ),
        (
            "ratency(0, 2)",
            new RateLatencyServiceCurve(0, 2)
        ),
        (
            "ratency(0, 0)",
            new RateLatencyServiceCurve(0, 0)
        ),
        (
            "ratency(0, 0)",
            Curve.Zero()
        ),
        (
            "bucket(2, 3)",
            new SigmaRhoArrivalCurve(3, 2)
        ),
        (
            "affine(2, 3)",
            new Curve(
                new Sequence([
                    new Point(0, 3),
                    new Segment(0, 1, 3, 2)
                ]),
                0, 1, 2
            )
        ),
        (
            "affine(2, 0)",
            new Curve(
                new Sequence([
                    new Point(0, 0),
                    new Segment(0, 1, 0, 2)
                ]),
                0, 1, 2
            )
        ),
        (
            "affine(0, 2)",
            new Curve(
                new Sequence([
                    new Point(0, 2),
                    new Segment(0, 1, 2, 0)
                ]),
                0, 1, 0
            )
        ),
        (
            "step(5, 10)",
            new StepCurve(10, 5)
        ),
        (
            "step(0, 10)",
            new StepCurve(10, 0)
        ),
        (
            "stair(2, 3, 4)",
            new StairCurve(4, 3).DelayBy(2)
        ),
        (
            "stair(0, 3, 4)",
            new StairCurve(4, 3)
        ),
        // todo: constant functions are implicitly constructed, how to test them?
        (
            "delay(7)",
            new DelayServiceCurve(7)
        ),
        (
            "delay(0)",
            new DelayServiceCurve(0)
        ),
        (
            "zero",
            Curve.Zero()
        ),
        (
            "epsilon",
            Curve.PlusInfinite()
        ),
        (
            "upp( period ( [(0, 0) 0 (2, 0)[ [(2, 0) 1 (7, 5)] ](7, 5) 0 ( 12, 5 )[ ))",
            new Curve(
                new Sequence([
                    Point.Origin(),
                    Segment.Zero(0, 2),
                    Point.Zero(2),
                    new Segment(2, 7, 0, 1),
                    new Point(7, 5),
                    Segment.Constant(7, 12, 5)
                ]),
                0,
                12,
                5
            )
        )
    ];

    public static IEnumerable<object[]> KnownMppgCurveTestCases =>
        KnownMppgCurvePairs.ToXUnitTestCases();
    
    [Theory]
    [MemberData(nameof(KnownMppgCurveTestCases))]
    public void MppgCurveParsingEquivalence(string mppg, Curve expected)
    {
        var state = new State();
        var ie = ExpressionParsing.Parse(mppg, state);
        Assert.IsAssignableFrom<CurveExpression>(ie);
        var curve = ((CurveExpression)ie).Value;
        Assert.True(Curve.Equivalent(expected, curve));
    }
}