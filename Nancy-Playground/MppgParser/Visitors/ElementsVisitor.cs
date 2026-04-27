using Unipi.MppgParser.Grammar;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

/// <summary>
/// In MPPG, each end of a segment may be either inclusive or exclusive.
/// So, rather than just a <see cref="Segment"/>, they are a sequence of <see cref="Element"/>s, including <see cref="Point"/>s as well.
/// </summary>
public class ElementsVisitor : MppgBaseVisitor<IEnumerable<Element>>
{
    public override IEnumerable<Element> VisitSequence(Unipi.MppgParser.Grammar.MppgParser.SequenceContext context)
    {
        var elements = Enumerable.Empty<Element>();
        for (int i = 0; i < context.ChildCount; i++)
        {
            var elementContext = context.GetChild(i);
            var elementsParsed = elementContext.Accept(this);
            elements = elements.Concat(elementsParsed);
        }
        return elements;
    }

    public override IEnumerable<Element> VisitElement(Unipi.MppgParser.Grammar.MppgParser.ElementContext context)
    {
        if (context.ChildCount != 1)
            throw new Exception("Expected 1 child expression");

        var childContext = context.GetChild(0);
        var elements = childContext.Accept(this);
        return elements;
    }

    public override IEnumerable<Element> VisitPoint(Unipi.MppgParser.Grammar.MppgParser.PointContext context)
    {
        var pointVisitor = new PointVisitor();
        var point = context.Accept(pointVisitor);

        yield return point;
    }

    public override IEnumerable<Element> VisitSegment(Unipi.MppgParser.Grammar.MppgParser.SegmentContext context)
    {
        if(context.ChildCount != 1)
            throw new Exception("Expected 1 child expression");

        var segmentInnerContext = context.GetChild(0);
        var elements = segmentInnerContext.Accept(this);
        return elements;
    }

    public override IEnumerable<Element> VisitSegmentLeftClosedRightClosed(Unipi.MppgParser.Grammar.MppgParser.SegmentLeftClosedRightClosedContext context)
    {
        var segmentText = context.GetJoinedText();

        var leftPointContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.EndpointContext>(0);
        var slopeContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext>(0);
        var rightPointContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.EndpointContext>(1);

        var pointVisitor = new PointVisitor();
        var leftPoint = pointVisitor.Visit(leftPointContext);
        var rightPoint = pointVisitor.Visit(rightPointContext);

        if(leftPoint.Time.IsInfinite)
            throw new InvalidOperationException($"Left endpoint cannot be infinite: {segmentText}");

        if(slopeContext == null)
        {
            var slope = (rightPoint.Value - leftPoint.Value) / (rightPoint.Time - leftPoint.Time);
            var segment = new Segment(leftPoint.Time, rightPoint.Time, leftPoint.Value, slope);
            yield return leftPoint;
            yield return segment;
            if (!rightPoint.Time.IsPlusInfinite)
                yield return rightPoint;
        }
        else
        {
            var numberLiteralVisitor = new NumberLiteralVisitor();
            var slope = numberLiteralVisitor.Visit(slopeContext);

            Rational computedSlope;
            if (leftPoint.Value.IsInfinite || rightPoint.Value.IsInfinite)
            {
                if(leftPoint.Value == rightPoint.Value)
                    computedSlope = 0;
                else
                    throw new InvalidOperationException($"Invalid segment between {leftPoint} and {rightPoint}");
            }
            else
                computedSlope = (rightPoint.Value - leftPoint.Value) / (rightPoint.Time - leftPoint.Time);
            if(slope != computedSlope)
                throw new InvalidOperationException($"Specified slope does not match the slope computed from the endpoints: {segmentText}");

            var segment = new Segment(leftPoint.Time, rightPoint.Time, leftPoint.Value, slope);

            yield return leftPoint;
            yield return segment;
            if (!rightPoint.Time.IsPlusInfinite)
                yield return rightPoint;
        }
    }

    public override IEnumerable<Element> VisitSegmentLeftClosedRightOpen(Unipi.MppgParser.Grammar.MppgParser.SegmentLeftClosedRightOpenContext context)
    {
        var segmentText = context.GetJoinedText();

        var leftPointContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.EndpointContext>(0);
        var slopeContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext>(0);
        var rightPointContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.EndpointContext>(1);

        var pointVisitor = new PointVisitor();
        var leftPoint = pointVisitor.Visit(leftPointContext);
        var rightPoint = pointVisitor.Visit(rightPointContext);

        if(leftPoint.Time.IsInfinite)
            throw new InvalidOperationException($"Left endpoint cannot be infinite: {segmentText}");

        if(slopeContext == null)
        {
            // slope is implicit
            // this is only allowed when
            // - both endpoints are finite
            // - the right endpoint is infinite, but the segment is constant
            if(rightPoint.Time.IsPlusInfinite)
            {
                if(leftPoint.Value != rightPoint.Value)
                    throw new InvalidOperationException($"Cannot infer slope for segment with infinite right endpoint and different values at endpoints: {segmentText}");

                var segment = new Segment(leftPoint.Time, Rational.PlusInfinity, leftPoint.Value, Rational.Zero);

                yield return leftPoint;
                yield return segment;
            }
            else
            {
                var slope = (rightPoint.Value - leftPoint.Value) / (rightPoint.Time - leftPoint.Time);
                var segment = new Segment(leftPoint.Time, rightPoint.Time, leftPoint.Value, slope);
                yield return leftPoint;
                yield return segment;
            }
        }
        else
        {
            var numberLiteralVisitor = new NumberLiteralVisitor();
            var slope = numberLiteralVisitor.Visit(slopeContext);

            if(rightPoint.Time.IsPlusInfinite)
            {
                if(slope < 0 && rightPoint.Value != Rational.MinusInfinity)
                    throw new InvalidOperationException($"Specified slope should lead to a minus infinite value at infinite time: {segmentText}");
                else if(slope > 0 && rightPoint.Value != Rational.PlusInfinity)
                    throw new InvalidOperationException($"Specified slope should lead to a plus infinite value at infinite time: {segmentText}");
                else if(slope == Rational.Zero && rightPoint.Value != leftPoint.Value)
                    throw new InvalidOperationException($"Specified slope should lead to a constant value at infinite time: {segmentText}");
            }
            else
            {
                Rational computedSlope;
                if (leftPoint.Value.IsInfinite || rightPoint.Value.IsInfinite)
                {
                    if(leftPoint.Value == rightPoint.Value)
                        computedSlope = 0;
                    else
                        throw new InvalidOperationException($"Invalid segment between {leftPoint} and {rightPoint}");
                }
                else
                    computedSlope = (rightPoint.Value - leftPoint.Value) / (rightPoint.Time - leftPoint.Time);
                if(slope != computedSlope)
                    throw new InvalidOperationException($"Specified slope does not match the slope computed from the endpoints: {segmentText}");
            }

            var segment = new Segment(leftPoint.Time, rightPoint.Time, leftPoint.Value, slope);
            yield return leftPoint;
            yield return segment;
        }
    }

    public override IEnumerable<Element> VisitSegmentLeftOpenRightClosed(Unipi.MppgParser.Grammar.MppgParser.SegmentLeftOpenRightClosedContext context)
    {
        var segmentText = context.GetJoinedText();

        var leftPointContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.EndpointContext>(0);
        var slopeContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext>(0);
        var rightPointContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.EndpointContext>(1);

        var pointVisitor = new PointVisitor();
        var leftPoint = pointVisitor.Visit(leftPointContext);
        var rightPoint = pointVisitor.Visit(rightPointContext);

        if(leftPoint.Time.IsInfinite)
            throw new InvalidOperationException($"Left endpoint cannot be infinite: {segmentText}");

        if(slopeContext == null)
        {
            var slope = (rightPoint.Value - leftPoint.Value) / (rightPoint.Time - leftPoint.Time);
            var segment = new Segment(leftPoint.Time, rightPoint.Time, leftPoint.Value, slope);
            yield return segment;
            if (!rightPoint.Time.IsPlusInfinite)
                yield return rightPoint;
        }
        else
        {
            var numberLiteralVisitor = new NumberLiteralVisitor();
            var slope = numberLiteralVisitor.Visit(slopeContext);

            Rational computedSlope;
            if (leftPoint.Value.IsInfinite || rightPoint.Value.IsInfinite)
            {
                if(leftPoint.Value == rightPoint.Value)
                    computedSlope = 0;
                else
                    throw new InvalidOperationException($"Invalid segment between {leftPoint} and {rightPoint}");
            }
            else
                computedSlope = (rightPoint.Value - leftPoint.Value) / (rightPoint.Time - leftPoint.Time);
            if(slope != computedSlope)
                throw new InvalidOperationException($"Specified slope does not match the slope computed from the endpoints: {segmentText}");

            var segment = new Segment(leftPoint.Time, rightPoint.Time, leftPoint.Value, slope);

            yield return segment;
            if (!rightPoint.Time.IsPlusInfinite)
                yield return rightPoint;
        }
    }

    public override IEnumerable<Element> VisitSegmentLeftOpenRightOpen(Unipi.MppgParser.Grammar.MppgParser.SegmentLeftOpenRightOpenContext context)
    {
        var segmentText = context.GetJoinedText();

        var leftPointContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.EndpointContext>(0);
        var slopeContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext>(0);
        var rightPointContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.EndpointContext>(1);

        var pointVisitor = new PointVisitor();
        var leftPoint = pointVisitor.Visit(leftPointContext);
        var rightPoint = pointVisitor.Visit(rightPointContext);

        if(leftPoint.Time.IsInfinite)
            throw new InvalidOperationException($"Left endpoint cannot be infinite: {segmentText}");

        if(slopeContext == null)
        {
            // slope is implicit
            // this is only allowed when
            // - both endpoints are finite
            // - the right endpoint is infinite, but the segment is constant
            if(rightPoint.Time.IsPlusInfinite)
            {
                if(leftPoint.Value != rightPoint.Value)
                    throw new InvalidOperationException($"Cannot infer slope for segment with infinite right endpoint and different values at endpoints: {segmentText}");

                var segment = new Segment(leftPoint.Time, Rational.PlusInfinity, leftPoint.Value, Rational.Zero);
                yield return segment;
            }
            else
            {
                var slope = (rightPoint.Value - leftPoint.Value) / (rightPoint.Time - leftPoint.Time);
                var segment = new Segment(leftPoint.Time, rightPoint.Time, leftPoint.Value, slope);
                yield return segment;
            }
        }
        else
        {
            var numberLiteralVisitor = new NumberLiteralVisitor();
            var slope = numberLiteralVisitor.Visit(slopeContext);

            if(rightPoint.Time.IsPlusInfinite)
            {
                if(slope < 0 && rightPoint.Value != Rational.MinusInfinity)
                    throw new InvalidOperationException($"Specified slope should lead to a minus infinite value at infinite time: {segmentText}");
                else if(slope > 0 && rightPoint.Value != Rational.PlusInfinity)
                    throw new InvalidOperationException($"Specified slope should lead to a plus infinite value at infinite time: {segmentText}");
                else if(slope == Rational.Zero && rightPoint.Value != leftPoint.Value)
                    throw new InvalidOperationException($"Specified slope should lead to a constant value at infinite time: {segmentText}");
            }
            else
            {
                Rational computedSlope;
                if (leftPoint.Value.IsInfinite || rightPoint.Value.IsInfinite)
                {
                    if(leftPoint.Value == rightPoint.Value)
                        computedSlope = 0;
                    else
                        throw new InvalidOperationException($"Invalid segment between {leftPoint} and {rightPoint}");
                }
                else
                    computedSlope = (rightPoint.Value - leftPoint.Value) / (rightPoint.Time - leftPoint.Time);
                if(slope != computedSlope)
                    throw new InvalidOperationException($"Specified slope does not match the slope computed from the endpoints: {segmentText}");
            }

            var segment = new Segment(leftPoint.Time, rightPoint.Time, leftPoint.Value, slope);
            yield return segment;
        }
    }
}