using Unipi.Nancy.Expressions;
using Unipi.Nancy.Expressions.Internals;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

public partial class ExpressionVisitor
{
    public override IExpression VisitFunctionBrackets(Unipi.MppgParser.Grammar.MppgParser.FunctionBracketsContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        return context.GetChild(1).Accept(this);
    }

    public override IExpression VisitNumberBrackets(Unipi.MppgParser.Grammar.MppgParser.NumberBracketsContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        return context.GetChild(1).Accept(this);
    }

    public override IExpression VisitEncNumberBrackets(Unipi.MppgParser.Grammar.MppgParser.EncNumberBracketsContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        return context.GetChild(1).Accept(this);
    }

    public override IExpression VisitFunctionMinPlusConvolution(
        Unipi.MppgParser.Grammar.MppgParser.FunctionMinPlusConvolutionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");
        var isNotationAmbiguous =  context.GetChild(1).GetText() == "*";

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, CurveExpression rCE):
            {
                var curveExp = Expressions.Expressions.Convolution(lCE, rCE);
                return curveExp;
            }
            case (RationalExpression lRE, RationalExpression rRE) when isNotationAmbiguous:
            {
                // this was mis-parsed: rational product
                var rationalExp = RationalExpression.Product(lRE, rRE);
                return rationalExp;
            }
            case (CurveExpression lCE, RationalExpression rRE) when isNotationAmbiguous:
            {
                // this was mis-parsed: function scalar multiplication
                var curveExp = lCE.Scale(rRE);
                return curveExp;
            }
            case (RationalExpression lRE, CurveExpression rCE) when isNotationAmbiguous:
            {
                // this was mis-parsed: function scalar multiplication
                var curveExp = rCE.Scale(lRE);
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }
        }
    }

    public override IExpression VisitFunctionMaxPlusConvolution(
        Unipi.MppgParser.Grammar.MppgParser.FunctionMaxPlusConvolutionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, CurveExpression rCE):
            {
                var curveExp = Expressions.Expressions.MaxPlusConvolution(lCE, rCE);
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }
        }
    }

    public override IExpression VisitFunctionMinPlusDeconvolution(
        Unipi.MppgParser.Grammar.MppgParser.FunctionMinPlusDeconvolutionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");
        var isNotationAmbiguous =  context.GetChild(1).GetText() == "/";

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, CurveExpression rCE):
            {
                var curveExp = Expressions.Expressions.Deconvolution(lCE, rCE);
                return curveExp;
            }
            case (RationalExpression lRE, RationalExpression rRE) when isNotationAmbiguous:
            {
                // this was mis-parsed: rational division
                var rationalExp = RationalExpression.Division(lRE, rRE);
                return rationalExp;
            }
            case (CurveExpression lCE, RationalExpression rRE) when isNotationAmbiguous:
            {
                // this was mis-parsed: function scalar division
                var curveExp = lCE.Scale(rRE.Invert());
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }
        }
    }

    public override IExpression VisitFunctionMaxPlusDeconvolution(
        Unipi.MppgParser.Grammar.MppgParser.FunctionMaxPlusDeconvolutionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, CurveExpression rCE):
            {
                var curveExp = Expressions.Expressions.MaxPlusDeconvolution(lCE, rCE);
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }
        }
    }

    public override IExpression VisitFunctionComposition(
        Unipi.MppgParser.Grammar.MppgParser.FunctionCompositionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, CurveExpression rCE):
            {
                var curveExp = Expressions.Expressions.Composition(lCE, rCE);
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }
        }
    }

    public override IExpression VisitFunctionScalarMultiplicationLeft(
        Unipi.MppgParser.Grammar.MppgParser.FunctionScalarMultiplicationLeftContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, RationalExpression rRE):
            {
                var curveExp = lCE.Scale(rRE);
                return curveExp;
            }
            case (CurveExpression lCE, CurveExpression rCE):
            {
                // this was mis-parsed: function min-plus convolution
                var curveExp = Expressions.Expressions.Convolution(lCE, rCE);
                return curveExp;
            }
            case (RationalExpression lRE, RationalExpression rRE):
            {
                // this was mis-parsed: rational product
                var rationalExp = RationalExpression.Product(lRE, rRE);
                return rationalExp;
            }
            case (RationalExpression lRE, CurveExpression rCE):
            {
                // this was mis-parsed: function scalar multiplication
                var curveExp = rCE.Scale(lRE);
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }
        }
    }

    public override IExpression VisitFunctionScalarMultiplicationRight(
        Unipi.MppgParser.Grammar.MppgParser.FunctionScalarMultiplicationRightContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, RationalExpression rRE):
            {
                var curveExp = lCE.Scale(rRE);
                return curveExp;
            }
            case (CurveExpression lCE, CurveExpression rCE):
            {
                // this was mis-parsed: function min-plus convolution
                var curveExp = Expressions.Expressions.Convolution(lCE, rCE);
                return curveExp;
            }
            case (RationalExpression lRE, RationalExpression rRE):
            {
                // this was mis-parsed: rational product
                var rationalExp = RationalExpression.Product(lRE, rRE);
                return rationalExp;
            }
            case (RationalExpression lRE, CurveExpression rCE):
            {
                // this was mis-parsed: function scalar multiplication
                var curveExp = rCE.Scale(lRE);
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }
        }
    }

    public override IExpression VisitFunctionScalarDivision(
        Unipi.MppgParser.Grammar.MppgParser.FunctionScalarDivisionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, RationalExpression rRE):
            {
                var curveExp = lCE.Scale(rRE.Invert());
                return curveExp;
            }
            case (RationalExpression lRE, RationalExpression rRE):
            {
                // this was mis-parsed: rational division
                var rationalExp = RationalExpression.Division(lRE, rRE);
                return rationalExp;
            }
            case (CurveExpression lCE, CurveExpression rCE):
            {
                // this was mis-parsed: function min-plus deconvolution
                var curveExp = Expressions.Expressions.Deconvolution(lCE, rCE);
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }
        }
    }

    public override IExpression VisitFunctionSumSubMinMax(Unipi.MppgParser.Grammar.MppgParser.FunctionSumSubMinMaxContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);
        var operation = context.op;

        switch (operation.Type)
        {
            case Unipi.MppgParser.Grammar.MppgParser.PLUS:
            {
                switch (ilE, irE)
                {
                    case (CurveExpression lCE, CurveExpression rCE):
                    {
                        var curveExp = Expressions.Expressions.Addition(lCE, rCE);
                        return curveExp;
                    }
                    case (CurveExpression lCE, RationalExpression rRE):
                    {
                        // this was mis-parsed
                        var curveExp = Expressions.Expressions.VerticalShift(lCE, rRE);
                        return curveExp;
                    }
                    case (RationalExpression lRE, CurveExpression rCE):
                    {
                        // this was mis-parsed
                        var curveExp = Expressions.Expressions.VerticalShift(rCE, lRE);
                        return curveExp;
                    }
                    case (RationalExpression lRE, RationalExpression rRE):
                    {
                        // this was mis-parsed
                        var rationalExp = RationalExpression.Addition(lRE, rRE);
                        return rationalExp;
                    }
                    default:
                    {
                        throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
                    }
                }
            }
                
            case Unipi.MppgParser.Grammar.MppgParser.MINUS:
            {
                switch (ilE, irE)
                {
                    case (CurveExpression lCE, CurveExpression rCE):
                    {
                        var curveExp = Expressions.Expressions.Subtraction(lCE, rCE);
                        return curveExp;
                    }
                    case (CurveExpression lCE, RationalExpression rRE):
                    {
                        // this was mis-parsed
                        var curveExp = Expressions.Expressions.VerticalShift(lCE, rRE.Negate());
                        return curveExp;
                    }
                    case (RationalExpression lRE, CurveExpression rCE):
                    {
                        // this was mis-parsed
                        var curveExp = Expressions.Expressions.VerticalShift(rCE, lRE.Negate());
                        return curveExp;
                    }
                    case (RationalExpression lRE, RationalExpression rRE):
                    {
                        // this was mis-parsed
                        var rationalExp = RationalExpression.Subtraction(lRE, rRE);
                        return rationalExp;
                    }
                    default:
                    {
                        throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
                    }
                }
            }
                
            case Unipi.MppgParser.Grammar.MppgParser.WEDGE:
            {
                switch (ilE, irE)
                {
                    case (CurveExpression lCE, CurveExpression rCE):
                    {
                        var curveExp = Expressions.Expressions.Minimum(lCE, rCE);
                        return curveExp;
                    }
                    case (RationalExpression lRE, RationalExpression rRE):
                    {
                        // this was mis-parsed
                        var rationalExp = RationalExpression.Min(lRE, rRE);
                        return rationalExp;
                    }
                    case (CurveExpression lCE, RationalExpression rRE):
                    {
                        var constantCurve = new PureConstantCurve(rRE.Compute());
                        var curveExp = Expressions.Expressions.Minimum(lCE, constantCurve);
                        return curveExp;
                    }
                    case (RationalExpression lRE, CurveExpression rCE):
                    {
                        var constantCurve = new PureConstantCurve(lRE.Compute());
                        var curveExp = Expressions.Expressions.Minimum(rCE, constantCurve);
                        return curveExp;
                    }
                    default:
                    {
                        throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
                    }
                }
            }
                
            case Unipi.MppgParser.Grammar.MppgParser.VEE:
            {
                switch (ilE, irE)
                {
                    case (CurveExpression lCE, CurveExpression rCE):
                    {
                        var curveExp = Expressions.Expressions.Maximum(lCE, rCE);
                        return curveExp;
                    }
                    case (RationalExpression lRE, RationalExpression rRE):
                    {
                        // this was mis-parsed
                        var rationalExp = RationalExpression.Max(lRE, rRE);
                        return rationalExp;
                    }
                    case (CurveExpression lCE, RationalExpression rRE):
                    {
                        var constantCurve = new PureConstantCurve(rRE.Compute());
                        var curveExp = Expressions.Expressions.Maximum(lCE, constantCurve);
                        return curveExp;
                    }
                    case (RationalExpression lRE, CurveExpression rCE):
                    {
                        var constantCurve = new PureConstantCurve(lRE.Compute());
                        var curveExp = Expressions.Expressions.Maximum(rCE, constantCurve);
                        return curveExp;
                    }
                    default:
                    {
                        throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
                    }
                }
            }
            
            default: 
                throw new InvalidOperationException($"Unexpected operation: {operation.Text}");
        }
    }
    
    public override IExpression VisitFunctionSubadditiveClosure(
        Unipi.MppgParser.Grammar.MppgParser.FunctionSubadditiveClosureContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = Expressions.Expressions.SubAdditiveClosure(lCE);
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    public override IExpression VisitFunctionHShift(Unipi.MppgParser.Grammar.MppgParser.FunctionHShiftContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var ifExp = context.GetChild(2).Accept(this);
        var ishiftExp = context.GetChild(4).Accept(this);

        if (ifExp is not CurveExpression fExp || ishiftExp is not RationalExpression shiftExp)
            throw new Exception("Expected f and shift expressions");

        var curveExp = fExp.HorizontalShift(shiftExp);
        return curveExp;
    }

    public override IExpression VisitFunctionVShift(Unipi.MppgParser.Grammar.MppgParser.FunctionVShiftContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var ifExp = context.GetChild(2).Accept(this);
        var ishiftExp = context.GetChild(4).Accept(this);

        if (ifExp is not CurveExpression fExp || ishiftExp is not RationalExpression shiftExp)
            throw new Exception("Expected f and shift expressions");

        var curveExp = fExp.VerticalShift(shiftExp);
        return curveExp;
    }

    public override IExpression VisitFunctionLowerPseudoInverse(Unipi.MppgParser.Grammar.MppgParser.FunctionLowerPseudoInverseContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = Expressions.Expressions.LowerPseudoInverse(lCE);
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    public override IExpression VisitFunctionUpperPseudoInverse(Unipi.MppgParser.Grammar.MppgParser.FunctionUpperPseudoInverseContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = Expressions.Expressions.UpperPseudoInverse(lCE);
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    public override IExpression VisitFunctionUpNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionUpNonDecreasingClosureContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = lCE.ToUpperNonDecreasing();
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    public override IExpression VisitFunctionNonNegativeUpNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionNonNegativeUpNonDecreasingClosureContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = lCE
                .ToNonNegative()
                .ToUpperNonDecreasing();
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    public override IExpression VisitFunctionLeftExt(Unipi.MppgParser.Grammar.MppgParser.FunctionLeftExtContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = lCE.ToLeftContinuous();
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    public override IExpression VisitFunctionRightExt(Unipi.MppgParser.Grammar.MppgParser.FunctionRightExtContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = lCE.ToRightContinuous();
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    public override IExpression VisitFunctionNegative(Unipi.MppgParser.Grammar.MppgParser.FunctionNegativeContext context)
    {
        var ie = base.VisitFunctionNegative(context);
        return ie switch
        {
            // shortcut for negated literals
            RationalNumberExpression rne => new RationalNumberExpression(-rne.Value),
            RationalExpression re => re.Negate(), // todo: support - operator
            CurveExpression ce => ce.Negate(), // todo: support - operator
            _ => throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"")
        };
    }
}