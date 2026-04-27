using Unipi.Nancy.Expressions;
using Unipi.Nancy.Expressions.Internals;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

public partial class ExpressionVisitor
{
    public override IExpression VisitNumberMulDiv(Unipi.MppgParser.Grammar.MppgParser.NumberMulDivContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);
        var operation = context.op;

        switch (operation.Type)
        {
            case Unipi.MppgParser.Grammar.MppgParser.PROD_SIGN:
            {
                switch (ilE, irE)
                {
                    case (RationalExpression lRE, RationalExpression rRE):
                    {
                        var rationalExp = RationalExpression.Product(lRE, rRE);
                        return rationalExp;
                    }
                    case (CurveExpression lCE, CurveExpression rCE):
                    {
                        // this was mis-parsed
                        var curveExp = Expressions.Expressions.Convolution(lCE, rCE);
                        return curveExp;
                    }
                    case (CurveExpression lCE, RationalExpression rRE):
                    {
                        // this was mis-parsed
                        var curveExp = Expressions.Expressions.Scale(lCE, rRE);
                        return curveExp;
                    }
                    default:
                    {
                        throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
                    }
                }
            }

            case Unipi.MppgParser.Grammar.MppgParser.DIV_SIGN:
            case Unipi.MppgParser.Grammar.MppgParser.DIV_OP:
            {
                switch (ilE, irE)
                {
                    case (RationalExpression lRE, RationalExpression rRE):
                    {
                        var rationalExp = RationalExpression.Division(lRE, rRE);
                        return rationalExp;
                    }
                    case (CurveExpression lCE, CurveExpression rCE):
                    {
                        // this was mis-parsed
                        var curveExp = Expressions.Expressions.Deconvolution(lCE, rCE);
                        return curveExp;
                    }
                    case (CurveExpression lCE, RationalExpression rRE):
                    {
                        // this was mis-parsed
                        var curveExp = Expressions.Expressions.Scale(lCE, rRE.Invert());
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
    
    public override IExpression VisitNumberSumSubMinMax(Unipi.MppgParser.Grammar.MppgParser.NumberSumSubMinMaxContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);
        var operation = context.op;

        // todo: can simplify this by narrowing operand types?
        switch (operation.Type)
        {
            case Unipi.MppgParser.Grammar.MppgParser.PLUS:
            {
                switch (ilE, irE)
                {
                    case (CurveExpression lCE, CurveExpression rCE):
                    {
                        // this was mis-parsed
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
                        // this was mis-parsed
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
                        // this was mis-parsed
                        var curveExp = Expressions.Expressions.Minimum(lCE, rCE);
                        return curveExp;
                    }
                    case (RationalExpression lRE, RationalExpression rRE):
                    {
                        var rationalExp = RationalExpression.Min(lRE, rRE);
                        return rationalExp;
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
                        // this was mis-parsed
                        var curveExp = Expressions.Expressions.Maximum(lCE, rCE);
                        return curveExp;
                    }
                    case (RationalExpression lRE, RationalExpression rRE):
                    {
                        var rationalExp = RationalExpression.Max(lRE, rRE);
                        return rationalExp;
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

    public override IExpression VisitNumberNegative(Unipi.MppgParser.Grammar.MppgParser.NumberNegativeContext context)
    {
        var ie = base.VisitNumberNegative(context);
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