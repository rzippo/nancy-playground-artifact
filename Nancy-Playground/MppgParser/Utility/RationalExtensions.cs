using System.Text;
using Unipi.Nancy.Numerics;

namespace Unipi.Nancy.Playground.MppgParser.Utility;

public static class RationalExtensions
{
    // todo: consider including these into Unipi.Nancy.Numerics

    /// <summary>
    /// Returns a string representing the explicit code to create the given Rational.
    /// It is explicit in the sense that it uses the Rational constructor, instead of implicit conversions from int.
    /// </summary>
    public static string ToExplicitCodeString(this Rational r)
    {
        var sb = new StringBuilder();
        sb.Append("new Rational(");
        sb.Append(r.Numerator.ToString());
        if (r.Denominator != 1)
        {
            sb.Append(", ");
            sb.Append(r.Denominator.ToString());
        }
        sb.Append(")");

        return sb.ToString();
    }

    /// <summary>
    /// Returns a pretty string representation of the Rational.
    /// If the Rational is infinite, it returns "Infinity" or "-Infinity" instead of 1/0 or -1/0.
    /// </summary>
    public static string ToPrettyString(this Rational r)
    {
        if (r.IsFinite)
            return r.ToString();
        else
            return $"{(r.Sign == 1 ? '+' : '-')}Infinity";
    }
}