using Unipi.Nancy.Expressions;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

public partial class ExpressionVisitor
{
    public override IExpression VisitFunctionValueAt(Unipi.MppgParser.Grammar.MppgParser.FunctionValueAtContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var functionNameContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.FunctionNameContext>(0);
        var functionName = functionNameContext.GetText();
        var curveExpr = State.GetFunctionVariable(functionName);

        var timeExpression = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(0);
        var iRE = timeExpression.Accept(this);
        if (iRE is RationalExpression re)
        {
            var valueAtExpr = curveExpr.ValueAt(re);
            return valueAtExpr;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    public override IExpression VisitFunctionLeftLimitAt(Unipi.MppgParser.Grammar.MppgParser.FunctionLeftLimitAtContext context)
    {
        // if (context.ChildCount != 5)
        //     throw new Exception("Expected 5 child expression");

        var functionNameContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.FunctionNameContext>(0);
        var functionName = functionNameContext.GetText();
        var curveExpr = State.GetFunctionVariable(functionName);

        var timeExpression = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(0);
        var iRE = timeExpression.Accept(this);
        if (iRE is RationalExpression re)
        {
            var valueAtExpr = curveExpr.LeftLimitAt(re);
            return valueAtExpr;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    public override IExpression VisitFunctionRightLimitAt(Unipi.MppgParser.Grammar.MppgParser.FunctionRightLimitAtContext context)
    {
        // if (context.ChildCount != 5)
        //     throw new Exception("Expected 5 child expression");

        var functionNameContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.FunctionNameContext>(0);
        var functionName = functionNameContext.GetText();
        var curveExpr = State.GetFunctionVariable(functionName);

        var timeExpression = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(0);
        var iRE = timeExpression.Accept(this);
        if (iRE is RationalExpression re)
        {
            var valueAtExpr = curveExpr.RightLimitAt(re);
            return valueAtExpr;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    public override IExpression VisitFunctionHorizontalDeviation(
        Unipi.MppgParser.Grammar.MppgParser.FunctionHorizontalDeviationContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var ilE = context.GetChild(2).Accept(this);
        var irE = context.GetChild(4).Accept(this);

        if (ilE is CurveExpression lCE && irE is CurveExpression rCE)
        {
            var rationalExp = Expressions.Expressions.HorizontalDeviation(lCE, rCE);
            return rationalExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    public override IExpression VisitFunctionVerticalDeviation(
        Unipi.MppgParser.Grammar.MppgParser.FunctionVerticalDeviationContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var ilE = context.GetChild(2).Accept(this);
        var irE = context.GetChild(4).Accept(this);

        if (ilE is CurveExpression lCE && irE is CurveExpression rCE)
        {
            var rationalExp = Expressions.Expressions.VerticalDeviation(lCE, rCE);
            return rationalExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }
}