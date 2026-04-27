using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

/// <summary>
/// $f(t) = K$, including in 0.
/// Should be added in Nancy? 
/// </summary>
internal class PureConstantCurve : Curve
{
    /// <summary>
    /// Value of the curve for any t
    /// </summary>
    public Rational Value { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    public PureConstantCurve(Rational value)
        : base(
            baseSequence: BuildSequence(value),
            pseudoPeriodStart: 0,
            pseudoPeriodLength: DefaultPeriodLength,
            pseudoPeriodHeight: 0
        )
    {
        Value = value;
    }

    /// <summary>
    /// Builds the sequence for the base class constructor
    /// </summary>
    internal static Sequence BuildSequence(Rational value)
    {
        if (value.IsFinite)
        {
            return new Sequence(
                new Element[]
                {
                    new Point(0, value),
                    Segment.Constant(0,
                        DefaultPeriodLength, value),
                });
        }
        else
        {
            return new Sequence(
                new Element[]
                {
                    Point.PlusInfinite(0),
                    Segment.PlusInfinite(
                        0,
                        DefaultPeriodLength)
                });
        }
    }

    internal static readonly Rational DefaultPeriodLength = 1;
}