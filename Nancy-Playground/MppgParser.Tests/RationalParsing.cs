using Unipi.Nancy.Expressions;
using Unipi.Nancy.Numerics;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

public class RationalParsing
{
    private readonly ITestOutputHelper _testOutputHelper;

    public RationalParsing(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static List<(string mppg, Rational expected)> KnownMppgRationalPairs =
    [
        ( "0", new Rational(0) ),
        ( "1", new Rational(1) ),
        ( "-3", new Rational(-3) ),
        ( "3/2", new Rational(3, 2) ),
        ( "-3/2", new Rational(-3, 2) ),
        ( "0.25", new Rational(1, 4) ),
        ( "-0.25", new Rational(-1, 4) ),
        ( "3200/0.00025", new Rational(12800000) ),
        ( "+inf", Rational.PlusInfinity ),
        ( "-inf", Rational.MinusInfinity ),
        ( "+infinity", Rational.PlusInfinity ),
        ( "-infinity", Rational.MinusInfinity ),
    ];

    public static IEnumerable<object[]> KnownMppgRationalTestCases =>
        KnownMppgRationalPairs.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(KnownMppgRationalTestCases))]
    public void MppgRationalParsingEquivalence(string mppg, Rational expected)
    {
        var state = new State();
        var ie = ExpressionParsing.Parse(mppg, state);
        Assert.IsAssignableFrom<RationalExpression>(ie);
        var curve = ((RationalExpression)ie).Value;
        Assert.Equal(expected, curve);
    }

}