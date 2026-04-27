using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;
class ExpressionTypeVisitor : MppgBaseVisitor<ExpressionType>
{
    public Dictionary<string, ExpressionType> State = new();
    
    public override ExpressionType VisitNumberVariableExp(Unipi.MppgParser.Grammar.MppgParser.NumberVariableExpContext context)
    {
        var name = context.GetChild(0).GetText();
        return State[name];
    }

    public override ExpressionType VisitEncNumberVariableExp(Unipi.MppgParser.Grammar.MppgParser.EncNumberVariableExpContext context)
    {
        var name = context.GetChild(0).GetText();
        return State[name];
    }

    public override ExpressionType VisitFunctionVariableExp(Unipi.MppgParser.Grammar.MppgParser.FunctionVariableExpContext context)
    {
        var name = context.GetChild(0).GetText();
        return State[name];
    }
    
    public override ExpressionType VisitFunctionName(Unipi.MppgParser.Grammar.MppgParser.FunctionNameContext context)
    {
        var name = context.GetChild(0).GetText();
        return State[name];
    }

    public override ExpressionType VisitNumberLiteral(Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext context)
    {
        return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionBrackets(Unipi.MppgParser.Grammar.MppgParser.FunctionBracketsContext context)
    {
        var innerType = context.GetChild(1).Accept(this);
        return innerType;
    }

    public override ExpressionType VisitNumberBrackets(Unipi.MppgParser.Grammar.MppgParser.NumberBracketsContext context)
    {
        var innerType = context.GetChild(1).Accept(this);
        return innerType;
    }

    #region Function binary operators

    public override ExpressionType VisitFunctionSumSubMinMax(Unipi.MppgParser.Grammar.MppgParser.FunctionSumSubMinMaxContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionMinPlusConvolution(Unipi.MppgParser.Grammar.MppgParser.FunctionMinPlusConvolutionContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            // either a convolution, or a function scaling
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionMaxPlusConvolution(Unipi.MppgParser.Grammar.MppgParser.FunctionMaxPlusConvolutionContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionMinPlusDeconvolution(Unipi.MppgParser.Grammar.MppgParser.FunctionMinPlusDeconvolutionContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            // either a deconvolution, or a function scaling
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionMaxPlusDeconvolution(Unipi.MppgParser.Grammar.MppgParser.FunctionMaxPlusDeconvolutionContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionComposition(Unipi.MppgParser.Grammar.MppgParser.FunctionCompositionContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionScalarMultiplicationLeft(Unipi.MppgParser.Grammar.MppgParser.FunctionScalarMultiplicationLeftContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            // either a convolution, or a function scaling
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }
    
    public override ExpressionType VisitFunctionScalarMultiplicationRight(Unipi.MppgParser.Grammar.MppgParser.FunctionScalarMultiplicationRightContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            // either a convolution, or a function scaling
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionScalarDivision(Unipi.MppgParser.Grammar.MppgParser.FunctionScalarDivisionContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            // either a deconvolution, or a function scaling
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }
    
    #endregion

    #region Function unary operators

    public override ExpressionType VisitFunctionSubadditiveClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionSubadditiveClosureContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionHShift(Unipi.MppgParser.Grammar.MppgParser.FunctionHShiftContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionVShift(Unipi.MppgParser.Grammar.MppgParser.FunctionVShiftContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionLowerPseudoInverse(Unipi.MppgParser.Grammar.MppgParser.FunctionLowerPseudoInverseContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionUpperPseudoInverse(Unipi.MppgParser.Grammar.MppgParser.FunctionUpperPseudoInverseContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionUpNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionUpNonDecreasingClosureContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionNonNegativeUpNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionNonNegativeUpNonDecreasingClosureContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionLeftExt(Unipi.MppgParser.Grammar.MppgParser.FunctionLeftExtContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionRightExt(Unipi.MppgParser.Grammar.MppgParser.FunctionRightExtContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    #endregion Function unary operators
    
    #region Function constructors

    public override ExpressionType VisitRateLatency(Unipi.MppgParser.Grammar.MppgParser.RateLatencyContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitTokenBucket(Unipi.MppgParser.Grammar.MppgParser.TokenBucketContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitAffineFunction(
        Unipi.MppgParser.Grammar.MppgParser.AffineFunctionContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }
    
    public override ExpressionType VisitStepFunction(
        Unipi.MppgParser.Grammar.MppgParser.StepFunctionContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitStairFunction(Unipi.MppgParser.Grammar.MppgParser.StairFunctionContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }
    
    public override ExpressionType VisitDelayFunction(
        Unipi.MppgParser.Grammar.MppgParser.DelayFunctionContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitZeroFunction(
        Unipi.MppgParser.Grammar.MppgParser.ZeroFunctionContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitEpsilonFunction(
        Unipi.MppgParser.Grammar.MppgParser.EpsilonFunctionContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    #endregion Function constructors

    #region Number-returning function operators

    public override ExpressionType VisitFunctionValueAt(Unipi.MppgParser.Grammar.MppgParser.FunctionValueAtContext context)
    {
        // not ambiguous
        return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionLeftLimitAt(Unipi.MppgParser.Grammar.MppgParser.FunctionLeftLimitAtContext context)
    {
        // not ambiguous
        return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionRightLimitAt(Unipi.MppgParser.Grammar.MppgParser.FunctionRightLimitAtContext context)
    {
        // not ambiguous
        return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionHorizontalDeviation(Unipi.MppgParser.Grammar.MppgParser.FunctionHorizontalDeviationContext context)
    {
        // not ambiguous
        return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionVerticalDeviation(Unipi.MppgParser.Grammar.MppgParser.FunctionVerticalDeviationContext context)
    {
        // not ambiguous
        return ExpressionType.Number;
    }

    #endregion
    
    #region Number binary operators

    public override ExpressionType VisitNumberMulDiv(Unipi.MppgParser.Grammar.MppgParser.NumberMulDivContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            // if op = *, either a convolution, or a function scaling;
            // if op = /, either a deconvolution, or a function scaling
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }

    public override ExpressionType VisitNumberSumSubMinMax(Unipi.MppgParser.Grammar.MppgParser.NumberSumSubMinMaxContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }

    #endregion
}