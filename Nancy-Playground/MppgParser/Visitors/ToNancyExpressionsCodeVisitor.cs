using System.Text;
using System.Text.RegularExpressions;
using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Expressions;
using Unipi.Nancy.Expressions.Internals;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Utility;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

/// <summary>
/// Converts an MPPG program to C# code that uses Nancy.Expressions constructs.
/// Equivalent to <see cref="ToNancyCodeVisitor"/> but generates code that works with
/// Nancy.Expressions API (CurveExpression, RationalExpression) instead of direct Curve/Rational objects.
/// </summary>
class ToNancyExpressionsCodeVisitor : MppgBaseVisitor<List<string>>
{
    private ExpressionTypeVisitor TypeVisitor { get; set; } = new();
    
    public override List<string> VisitProgram(Unipi.MppgParser.Grammar.MppgParser.ProgramContext context)
    {
        var statementLineContexts = context.GetRuleContexts<Unipi.MppgParser.Grammar.MppgParser.StatementLineContext>();

        List<string> code = [
            "#:package Unipi.Nancy.Expressions@1.0.0",
            "#:package Unipi.Nancy.Plots.ScottPlot@1.0.4",
            string.Empty,
            "using System.Globalization;",
            "using System.IO;",
            "using Unipi.Nancy.Expressions;",
            "using Unipi.Nancy.NetworkCalculus;",
            "using Unipi.Nancy.MinPlusAlgebra;",
            "using Unipi.Nancy.Numerics;",
            "using Unipi.Nancy.Plots.ScottPlot;",
            string.Empty,
            "CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;",
        ];
        
        foreach (var statementLineContext in statementLineContexts)
        {
            var statementLineCode = statementLineContext.Accept(this);
            if (statementLineCode.Count <= 0) continue;
            code.Add(string.Empty);
            code.AddRange(statementLineCode);
        }

        code.Add(string.Empty);
        code.Add("// END OF PROGRAM");

        code = CleanupReassignments(code);
        
        return code;
    }

    public override List<string> VisitStatementLine(Unipi.MppgParser.Grammar.MppgParser.StatementLineContext context)
    {
        var statementContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.StatementContext>(0);
        var inlineCommentContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.InlineCommentContext>(0);

        if (statementContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.EmptyContext>(0) is not null)
        {
            return [];
        }
        if (statementContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.CommentContext>(0) is not null)
        {
            var comment = statementContext.Accept(this).Single();
            if (inlineCommentContext != null)
            {
                var inlineComment = inlineCommentContext.GetJoinedText();
                comment = $"{comment} {inlineComment}";
            }
            return [comment];
        }
        else
        {
            List<string> code = [
                $"// code for: {context.GetJoinedText()}"
            ];
            
            var statementCode = statementContext.Accept(this);
            if(statementCode != null)
            {
                if (inlineCommentContext != null)
                {
                    var inlineComment = inlineCommentContext.GetJoinedText();
                    statementCode[^1] = $"{statementCode[^1]} // {inlineComment}";
                }
                code.AddRange(statementCode);
            }
            else
            {
                code.Add("// NOT IMPLEMENTED");
            }
            
            return code;
        }
    }

    public override List<string> VisitAssignment(Unipi.MppgParser.Grammar.MppgParser.AssignmentContext context)
    {
        var name = context.GetChild(0).GetText();
        var expressionContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.ExpressionContext>(0);
        
        var expressionCode = expressionContext.Accept(this);
        var expressionType = expressionContext.Accept(TypeVisitor);
        var lhs = TypeVisitor.State.ContainsKey(name) ? $"{name}" : $"var {name}";
        List<string> result;
        if (expressionCode is null || expressionCode.Count == 0)
            // throw new InvalidOperationException("Expression code empty");
            result = [$"// {lhs} = ...;"];
        else if (expressionCode.Count == 1)
            result = [$"{lhs} = {expressionCode.Single()};"];
        else
        {
            expressionCode[^1] = $"{lhs} = {expressionCode[^1]};";
            result = expressionCode;
        }

        TypeVisitor.State[name] = expressionType;
        return result;
    }

    public override List<string> VisitExpressionCommand(Unipi.MppgParser.Grammar.MppgParser.ExpressionCommandContext context)
    {
        var expressionContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.ExpressionContext>(0);
        var expression = expressionContext.Accept(this).Single();

        return [$"Console.WriteLine({expression}.Compute());"];
    }

    public override List<string> VisitPlotCommand(Unipi.MppgParser.Grammar.MppgParser.PlotCommandContext context)
    {
        var text = context.GetJoinedText();
        var args = context.GetRuleContexts<Unipi.MppgParser.Grammar.MppgParser.PlotArgContext>();

        var functionNameContexts = args
            .Select(arg => arg.GetChild<Unipi.MppgParser.Grammar.MppgParser.FunctionNameContext>(0))
            .Where(ctx => ctx != null);
        var plotOptionContexts = args
            .Select(arg => arg.GetChild<Unipi.MppgParser.Grammar.MppgParser.PlotOptionContext>(0))
            .Where(ctx => ctx != null);
        
        var functionsToPlot = functionNameContexts
            .Select(ctx => ctx.GetText())
            .ToList();
        
        var lines = new List<string>();
        // Need to extract Curve objects from CurveExpression for plotting
        lines.Add("var plotBytes = ScottPlots.ToScottPlotImage(");
        lines.Add($"\t[{functionsToPlot.Select(f => $"{f}.Compute()").JoinText(", ")}],");
        lines.Add("\tsettings: new ScottPlotSettings(){");
        
        var outPath = string.Empty;

        // populate then print dictionary, to mimic default values of PlotSettings
        var argsDict = new Dictionary<string, string>();
        argsDict["Title"] = "string.Empty";
        argsDict["XLabel"] = "string.Empty";
        argsDict["YLabel"] = "string.Empty";

        foreach (var plotArgContext in plotOptionContexts)
        {
            var argName = plotArgContext.GetChild(0).GetText();
            var argString = plotArgContext.GetChild(2).GetText()
                .TrimQuotes();

            switch (argName)
            {
                case "main":
                case "title":
                {
                    var stringContext = plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.StringContext>(0);
                    var formattableString = stringContext.Accept(this);
                    if (formattableString is not null && formattableString.Count == 1)
                        argsDict["Title"] = formattableString.Single();
                    break;
                }

                case "xlim":
                {
                    var intervalContext = plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.IntervalContext>(0);
                    var numberVisitor = new NumberLiteralVisitor();
                    var leftLimitContext = intervalContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext>(0);
                    var rightLimitContext = intervalContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext>(1);
                    var leftLimit = numberVisitor.Visit(leftLimitContext);
                    var rightLimit = numberVisitor.Visit(rightLimitContext);
                    argsDict["XLimit"] = $"new Interval({leftLimit.ToCodeString()}, {rightLimit.ToCodeString()})";
                    break;
                }

                case "ylim":
                {
                    var intervalContext = plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.IntervalContext>(0);
                    var numberVisitor = new NumberLiteralVisitor();
                    var leftLimitContext = intervalContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext>(0);
                    var rightLimitContext = intervalContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext>(1);
                    var leftLimit = numberVisitor.Visit(leftLimitContext);
                    var rightLimit = numberVisitor.Visit(rightLimitContext);
                    argsDict["YLimit"] = $"new Interval({leftLimit.ToCodeString()}, {rightLimit.ToCodeString()})";
                    break;
                }

                case "xlab":
                {
                    var stringContext = plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.StringContext>(0);
                    var formattableString = stringContext.Accept(this);
                    if (formattableString is not null && formattableString.Count == 1)
                        argsDict["XLabel"] = formattableString.Single();
                    break;
                }

                case "ylab":
                {
                    var stringContext = plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.StringContext>(0);
                    var formattableString = stringContext.Accept(this);
                    if (formattableString is not null && formattableString.Count == 1)
                        argsDict["YLabel"] = formattableString.Single();
                    break;
                }

                case "out":
                {
                    outPath = argString.EndsWith(".png") ? argString : $"{argString}.png";
                    break;
                }

                case "grid":
                {
                    // option not implemented in Nancy.Plots.ScottPlot
                    break;
                }

                case "bg":
                {
                    // option not implemented in Nancy.Plots.ScottPlot
                    break;
                }

                case "gui":
                {
                    // option not meaningful in convert
                    break;
                }
                
                default:
                    // do nothing
                    break;
            }
        }

        // now print the parsed arguments for ScottPlotSettings
        lines.AddRange(
            argsDict
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                .Select(kv => $"\t\t{kv.Key} = {kv.Value},")
        );

        lines.Add("\t}");
        lines.Add(");");

        if (!string.IsNullOrWhiteSpace(outPath))
        {
            lines.Add($"Console.WriteLine(Path.GetFullPath(\"{outPath}\"));");
            lines.Add($"File.WriteAllBytes(\"{outPath}\", plotBytes);");
        }
        else
        {
            lines.Add($"var plotTmpPath = Path.GetTempPath() + Guid.NewGuid().ToString() + \".png\";");
            lines.Add($"Console.WriteLine(plotTmpPath);");
            lines.Add($"File.WriteAllBytes(plotTmpPath, plotBytes);");
        }

        return lines;
    }

    public override List<string> VisitAssertion(Unipi.MppgParser.Grammar.MppgParser.AssertionContext context)
    {
        var leftExpressionContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.ExpressionContext>(0);
        var rightExpressionContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.ExpressionContext>(1);
        var operatorContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.AssertionOperatorContext>(0);

        var leftExpressionCode = leftExpressionContext.Accept(this);
        var rightExpressionCode = rightExpressionContext.Accept(this);
        var operatorText = operatorContext.GetText();

        if (leftExpressionCode is null || leftExpressionCode.Count == 0 ||
            rightExpressionCode is null || rightExpressionCode.Count == 0)
        {
            return [$"// INNER EXPRESSION NOT IMPLEMENTED"];
        }

        var leftExpr = leftExpressionCode.Single();
        var rightExpr = rightExpressionCode.Single();

        // Map assertion operators to C# comparison operators
        var csharpOperator = operatorText switch
        {
            "=" => "==",
            "!=" => "!=",
            "<" => "<",
            "<=" => "<=",
            ">" => ">",
            ">=" => ">=",
            _ => "=="
        };

        if(csharpOperator == "==")
        {
            // for function equality, must use Nancy's Curve.Equivalence to treat different representations of the same curve as equal
            var leftType = leftExpressionContext.Accept(TypeVisitor);
            var rightType = rightExpressionContext.Accept(TypeVisitor);
            if (leftType == ExpressionType.Function && rightType == ExpressionType.Function)
            {
                return [
                    $"Console.WriteLine(Curve.Equivalent({leftExpr}.Compute(), {rightExpr}.Compute()).ToString().ToLower());"
                ];
            }
        }

        // In all other cases, C# operators will do the job
        return [
            $"Console.WriteLine(({leftExpr}.Compute() {csharpOperator} {rightExpr}.Compute()).ToString().ToLower());"
        ];
    }

    public override List<string> VisitString(Unipi.MppgParser.Grammar.MppgParser.StringContext context)
    {
        var visitor = new ComputableStringVisitor();
        var cs = context.Accept(visitor);
        if (cs is null)
            return [];
        else
        {
            var sb = new StringBuilder("$\"");
            foreach (var piece in cs.Pieces)
            {
                if (piece is string s)
                    sb.Append(s);
                else if (piece is Expression e)
                    sb.Append($"{{{e.VariableName}}}");
                else
                    sb.Append($"{{{piece}}}");
            }
            sb.Append("\"");
            return [sb.ToString()];
        }
    }

    public override List<string> VisitNumberVariableExp(Unipi.MppgParser.Grammar.MppgParser.NumberVariableExpContext context)
    {
        var name = context.GetChild(0).GetText();
        return [name];
    }

    public override List<string> VisitEncNumberVariableExp(Unipi.MppgParser.Grammar.MppgParser.EncNumberVariableExpContext context)
    {
        var name = context.GetChild(0).GetText();
        return [name];
    }

    public override List<string> VisitComment(Unipi.MppgParser.Grammar.MppgParser.CommentContext context)
    {
        var text = context.GetJoinedText();
        return [$"// {text}"];
    }

    public override List<string> VisitFunctionVariableExp(Unipi.MppgParser.Grammar.MppgParser.FunctionVariableExpContext context)
    {
        var name = context.GetChild(0).GetText();
        return [name];
    }
    
    public override List<string> VisitFunctionName(Unipi.MppgParser.Grammar.MppgParser.FunctionNameContext context)
    {
        var name = context.GetChild(0).GetText();
        return [name];
    }

    public override List<string> VisitNumberLiteral(Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext context)
    {
        var visitor = new NumberLiteralVisitor();
        var number = context.Accept(visitor);
        // todo: this may be simplifiable in many cases
        return [$"Expressions.FromRational({number.ToExplicitCodeString()})"];
    }

    public override List<string> VisitFunctionBrackets(Unipi.MppgParser.Grammar.MppgParser.FunctionBracketsContext context)
    {
        var innerCode = context.GetChild(1).Accept(this).Single();
        return [$"( {innerCode} )"];
    }

    public override List<string> VisitNumberBrackets(Unipi.MppgParser.Grammar.MppgParser.NumberBracketsContext context)
    {
        var innerCode = context.GetChild(1).Accept(this).Single();
        return [$"( {innerCode} )"];
    }

    #region Function binary operators

    public override List<string> VisitFunctionSumSubMinMax(Unipi.MppgParser.Grammar.MppgParser.FunctionSumSubMinMaxContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();
        var operation = context.op;

        switch (operation.Type)
        {
            case Unipi.MppgParser.Grammar.MppgParser.PLUS:
            {
                return [$"{first} + {second}"];
            }
                
            case Unipi.MppgParser.Grammar.MppgParser.MINUS:
            {
                return [$"{first} - {second}"];
            }
                
            case Unipi.MppgParser.Grammar.MppgParser.WEDGE:
            {
                var firstType = context.GetChild(0).Accept(TypeVisitor);
                var secondType = context.GetChild(2).Accept(TypeVisitor);
        
                if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
                    return [$"{first}.Minimum({second})"];
                else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
                {
                    var secondContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(0);
                    var secondWrapped = secondContext is not null ? WrapRationalExpressionIfNeeded(secondContext) : WrapRationalExpression(second);
                    return [$"{first}.Minimum(new Curve(new Sequence([ new Point(0, {secondWrapped}), Segment.Constant(0, 1, {secondWrapped})]), 0, 1, 0))"];
                }
                else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
                {
                    var firstContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(0);
                    var firstWrapped = firstContext is not null ? WrapRationalExpressionIfNeeded(firstContext) : WrapRationalExpression(first);
                    return [$"{second}.Minimum(new Curve(new Sequence([ new Point(0, {firstWrapped}), Segment.Constant(0, 1, {firstWrapped})]), 0, 1, 0))"];
                }
                else
                    return [$"RationalExpression.Min({first}, {second})"];
            }
                
            case Unipi.MppgParser.Grammar.MppgParser.VEE:
            {
                var firstType = context.GetChild(0).Accept(TypeVisitor);
                var secondType = context.GetChild(2).Accept(TypeVisitor);
        
                if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
                    return [$"{first}.Maximum({second})"];
                else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
                {
                    var secondContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(0);
                    var secondWrapped = secondContext is not null ? WrapRationalExpressionIfNeeded(secondContext) : WrapRationalExpression(second);
                    return [$"{first}.Maximum(new Curve(new Sequence([ new Point(0, {secondWrapped}), Segment.Constant(0, 1, {secondWrapped})]), 0, 1, 0))"];
                }
                else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
                {
                    var firstContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(0);
                    var firstWrapped = firstContext is not null ? WrapRationalExpressionIfNeeded(firstContext) : WrapRationalExpression(first);
                    return [$"{second}.Maximum(new Curve(new Sequence([ new Point(0, {firstWrapped}), Segment.Constant(0, 1, {firstWrapped})]), 0, 1, 0))"];
                }
                else
                    return [$"RationalExpression.Max({first}, {second})"];
            }
            
            default: 
                throw new InvalidOperationException($"Unexpected operation: {operation.Text}");
        }
    }
    
    public override List<string> VisitFunctionMinPlusConvolution(Unipi.MppgParser.Grammar.MppgParser.FunctionMinPlusConvolutionContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        var firstType = context.GetChild(0).Accept(TypeVisitor);
        var secondType = context.GetChild(2).Accept(TypeVisitor);
        
        if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
            return [$"{first}.Convolution({second})"];
        else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
            return [$"{first} * {second}"];
        else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
            return [$"{second} * {first}"];
        else
            return [$"{first} * {second}"];
    }

    public override List<string> VisitFunctionMaxPlusConvolution(Unipi.MppgParser.Grammar.MppgParser.FunctionMaxPlusConvolutionContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        return [$"{first}.MaxPlusConvolution({second})"];
    }

    public override List<string> VisitFunctionMinPlusDeconvolution(Unipi.MppgParser.Grammar.MppgParser.FunctionMinPlusDeconvolutionContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        var firstType = context.GetChild(0).Accept(TypeVisitor);
        var secondType = context.GetChild(2).Accept(TypeVisitor);
        
        if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
            return [$"{first}.Deconvolution({second})"];
        else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
            return [$"{first} / {second}"];
        else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
            throw new InvalidOperationException($"Unexpected expression type: {context.GetJoinedText()}");
        else
            return [$"{first} / {second}"];
    }

    public override List<string> VisitFunctionMaxPlusDeconvolution(Unipi.MppgParser.Grammar.MppgParser.FunctionMaxPlusDeconvolutionContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        return [$"{first}.MaxPlusDeconvolution({second})"];
    }

    public override List<string> VisitFunctionComposition(Unipi.MppgParser.Grammar.MppgParser.FunctionCompositionContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        return [$"{first}.Composition({second})"];
    }

    public override List<string> VisitFunctionScalarMultiplicationLeft(Unipi.MppgParser.Grammar.MppgParser.FunctionScalarMultiplicationLeftContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        var firstType = context.GetChild(0).Accept(TypeVisitor);
        var secondType = context.GetChild(2).Accept(TypeVisitor);
        
        if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
            return [$"{first}.Convolution({second})"];
        else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
            return [$"{first} * {second}"];
        else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
            return [$"{second} * {first}"];
        else
            return [$"{first} * {second}"];
    }
    
    public override List<string> VisitFunctionScalarMultiplicationRight(Unipi.MppgParser.Grammar.MppgParser.FunctionScalarMultiplicationRightContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        var firstType = context.GetChild(0).Accept(TypeVisitor);
        var secondType = context.GetChild(2).Accept(TypeVisitor);
        
        if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
            return [$"{first}.Convolution({second})"];
        else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
            return [$"{first} * {second}"];
        else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
            return [$"{second} * {first}"];
        else
            return [$"{first} * {second}"];
    }

    public override List<string> VisitFunctionScalarDivision(Unipi.MppgParser.Grammar.MppgParser.FunctionScalarDivisionContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        var firstType = context.GetChild(0).Accept(TypeVisitor);
        var secondType = context.GetChild(2).Accept(TypeVisitor);
        
        if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
            return [$"{first}.Deconvolution({second})"];
        else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
            return [$"{first} / {second}"];
        else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
            throw new InvalidOperationException($"Unexpected expression type: {context.GetJoinedText()}");
        else
            return [$"{first} / {second}"];
    }

    #endregion

    #region Function unary operators

    public override List<string> VisitFunctionSubadditiveClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionSubadditiveClosureContext context)
    {
        var curve = context.GetChild(2).Accept(this).Single();

        return [$"({curve}).SubAdditiveClosure()"];
    }

    public override List<string> VisitFunctionHShift(Unipi.MppgParser.Grammar.MppgParser.FunctionHShiftContext context)
    {
        var curve = context.GetChild(2).Accept(this).Single();
        var shift = context.GetChild(4).Accept(this).Single();

        return [$"({curve}).HorizontalShift({shift})"];
    }

    public override List<string> VisitFunctionVShift(Unipi.MppgParser.Grammar.MppgParser.FunctionVShiftContext context)
    {
        var curve = context.GetChild(2).Accept(this).Single();
        var shift = context.GetChild(4).Accept(this).Single();

        return [$"({curve}).VerticalShift({shift})"];
    }

    public override List<string> VisitFunctionLowerPseudoInverse(Unipi.MppgParser.Grammar.MppgParser.FunctionLowerPseudoInverseContext context)
    {
        var curve = context.GetChild(2).Accept(this).Single();

        return [$"({curve}).LowerPseudoInverse()"];
    }

    public override List<string> VisitFunctionUpperPseudoInverse(Unipi.MppgParser.Grammar.MppgParser.FunctionUpperPseudoInverseContext context)
    {
        var curve = context.GetChild(2).Accept(this).Single();

        return [$"({curve}).UpperPseudoInverse()"];
    }

    public override List<string> VisitFunctionUpNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionUpNonDecreasingClosureContext context)
    {
        var curve = context.GetChild(2).Accept(this).Single();
        
        return [$"({curve}).ToUpperNonDecreasing()"];
    }

    public override List<string> VisitFunctionNonNegativeUpNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionNonNegativeUpNonDecreasingClosureContext context)
    {
        var curve = context.GetChild(2).Accept(this).Single();
        
        return [$"({curve}).ToNonNegative().ToUpperNonDecreasing()"];
    }

    public override List<string> VisitFunctionLeftExt(Unipi.MppgParser.Grammar.MppgParser.FunctionLeftExtContext context)
    {
        var curve = context.GetChild(2).Accept(this).Single();
        
        return [$"({curve}).ToLeftContinuous()"];
    }

    public override List<string> VisitFunctionRightExt(Unipi.MppgParser.Grammar.MppgParser.FunctionRightExtContext context)
    {
        var curve = context.GetChild(2).Accept(this).Single();
        
        return [$"({curve}).ToRightContinuous()"];
    }

    #endregion Function unary operators
    
    #region Function constructors

    /// <summary>
    /// Determines if a context expression is a literal rational or a variable reference / computed value that resolves to a RationalExpression.
    /// In the latter case, the generated code wraps with .Compute().
    /// </summary>
    private string WrapRationalExpressionIfNeeded(Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext context)
    {
        // todo: simplify code by omitting parentheses when not needed
        var expressionCode = context.Accept(this).Single(); 
        
        try {
            // Try to determine the actual type via ExpressionVisitor
            var expressionVisitor = new ExpressionVisitor(null);
            var expression = context.Accept(expressionVisitor);

            if(expression is RationalNumberExpression re)
                return re.Value.ToExplicitCodeString();
            else
                return WrapRationalExpression(expressionCode);
        }
        catch
        {
            // Fallback: if any error occurs, assume we need to wrap with .Compute()
            // Example: variable reference that is not known at this time
            return WrapRationalExpression(expressionCode);
        }
    }

    /// <summary>
    /// Determines if a context expression is a literal rational or a variable reference / computed value that resolves to a RationalExpression.
    /// In the latter case, the generated code wraps with .Compute().
    /// </summary>
    private string WrapRationalExpression(string expressionCode)
    {
        // todo: simplify code by omitting parentheses when not needed
        return $"({expressionCode}).Compute()";
    }

    public override List<string> VisitRateLatency(Unipi.MppgParser.Grammar.MppgParser.RateLatencyContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var rateContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(0);
        var latencyContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(1);
        
        var rate = WrapRationalExpressionIfNeeded(rateContext ?? throw new Exception("Expected rate expression"));
        var latency = WrapRationalExpressionIfNeeded(latencyContext ?? throw new Exception("Expected latency expression"));

        return [$"Expressions.FromCurve(new RateLatencyServiceCurve({rate}, {latency}), name: \"ratency\")"];
    }

    public override List<string> VisitTokenBucket(Unipi.MppgParser.Grammar.MppgParser.TokenBucketContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var aContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(0);
        var bContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(1);
        
        var a = WrapRationalExpressionIfNeeded(aContext ?? throw new Exception("Expected a expression"));
        var b = WrapRationalExpressionIfNeeded(bContext ?? throw new Exception("Expected b expression"));

        return [$"Expressions.FromCurve(new SigmaRhoArrivalCurve({b}, {a}), name: \"bucket\")"];
    }

    public override List<string> VisitAffineFunction(
        Unipi.MppgParser.Grammar.MppgParser.AffineFunctionContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var slopeContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(0);
        var constantContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(1);
        
        var slope = WrapRationalExpressionIfNeeded(slopeContext ?? throw new Exception("Expected slope expression"));
        var constant = WrapRationalExpressionIfNeeded(constantContext ?? throw new Exception("Expected constant expression"));

        return [$"Expressions.FromCurve(new Curve(new Sequence([new Point(0, {constant}), new Segment(0, 1, {constant}, {slope}) ]), 0, 1, {slope}), name: \"affine\")"];
    }
    
    public override List<string> VisitStepFunction(
        Unipi.MppgParser.Grammar.MppgParser.StepFunctionContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var oContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(0);
        var hContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(1);
        
        var o = WrapRationalExpressionIfNeeded(oContext ?? throw new Exception("Expected o expression"));
        var h = WrapRationalExpressionIfNeeded(hContext ?? throw new Exception("Expected h expression"));

        return [$"Expressions.FromCurve(new StepCurve({h}, {o}), name: \"step\")"];
    }

    public override List<string> VisitStairFunction(Unipi.MppgParser.Grammar.MppgParser.StairFunctionContext context)
    {
        if (context.ChildCount != 8)
            throw new Exception("Expected 8 child expression");

        var oContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(0);
        var lContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(1);
        var hContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(2);
        
        var o = WrapRationalExpressionIfNeeded(oContext ?? throw new Exception("Expected o expression"));
        var l = WrapRationalExpressionIfNeeded(lContext ?? throw new Exception("Expected l expression"));
        var h = WrapRationalExpressionIfNeeded(hContext ?? throw new Exception("Expected h expression"));

        return [$"Expressions.FromCurve(new StairCurve({h}, {l}).DelayBy({o}), name: \"stair\")"];
    }
    
    public override List<string> VisitDelayFunction(
        Unipi.MppgParser.Grammar.MppgParser.DelayFunctionContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var dContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(0);
        var d = WrapRationalExpressionIfNeeded(dContext ?? throw new Exception("Expected d expression"));

        return [$"Expressions.FromCurve(new DelayServiceCurve({d}), name: \"delay\")"];
    }

    public override List<string> VisitZeroFunction(
        Unipi.MppgParser.Grammar.MppgParser.ZeroFunctionContext context)
    {
        return ["Expressions.FromCurve(Curve.Zero(), name: \"zero\")"];
    }

    public override List<string> VisitEpsilonFunction(
        Unipi.MppgParser.Grammar.MppgParser.EpsilonFunctionContext context)
    {
        return ["Expressions.FromCurve(Curve.PlusInfinite(), name: \"epsilon\")"];
    }

    public override List<string> VisitUltimatelyAffineFunction(Unipi.MppgParser.Grammar.MppgParser.UltimatelyAffineFunctionContext context)
    {
        // reuse the actual parsing + ToCodeString()
        var expressionVisitor = new ExpressionVisitor(null);
        var expression = context.Accept(expressionVisitor);

        if (expression is ConcreteCurveExpression ce)
        {
            var curve = ce.Value;
            return [$"Expressions.FromCurve({curve.ToCodeString()}, name: \"uaf\")"];
        }
        else
        {
            throw new InvalidOperationException("Expected ConcreteCurveExpression");
        }
    }

    public override List<string> VisitUltimatelyPseudoPeriodicFunction(Unipi.MppgParser.Grammar.MppgParser.UltimatelyPseudoPeriodicFunctionContext context)
    {
        // reuse the actual parsing + ToCodeString()
        var expressionVisitor = new ExpressionVisitor(null);
        var expression = context.Accept(expressionVisitor);

        if (expression is ConcreteCurveExpression ce)
        {
            var curve = ce.Value;
            return [$"Expressions.FromCurve({curve.ToCodeString()}, name: \"upp\")"];
        }
        else
        {
            throw new InvalidOperationException("Expected ConcreteCurveExpression");
        }
    }

    #endregion Function constructors

    #region Number-returning function operators

    public override List<string> VisitFunctionValueAt(Unipi.MppgParser.Grammar.MppgParser.FunctionValueAtContext context)
    {
        var curve = context.GetChild(0).Accept(this).Single();
        var time = context.GetChild(2).Accept(this).Single();

        return [$"{curve}.ValueAt({time})"];
    }

    public override List<string> VisitFunctionLeftLimitAt(Unipi.MppgParser.Grammar.MppgParser.FunctionLeftLimitAtContext context)
    {
        var curve = context.GetChild(0).Accept(this).Single();
        var time = context.GetChild(2).Accept(this).Single();

        return [$"{curve}.LeftLimitAt({time})"];
    }

    public override List<string> VisitFunctionRightLimitAt(Unipi.MppgParser.Grammar.MppgParser.FunctionRightLimitAtContext context)
    {
        var curve = context.GetChild(0).Accept(this).Single();
        var time = context.GetChild(2).Accept(this).Single();

        return [$"{curve}.RightLimitAt({time})"];
    }

    public override List<string> VisitFunctionHorizontalDeviation(Unipi.MppgParser.Grammar.MppgParser.FunctionHorizontalDeviationContext context)
    {
        var l = context.GetChild(2).Accept(this).Single();
        var r = context.GetChild(4).Accept(this).Single();

        return [$"Expressions.HorizontalDeviation({l}, {r})"];
    }

    public override List<string> VisitFunctionVerticalDeviation(Unipi.MppgParser.Grammar.MppgParser.FunctionVerticalDeviationContext context)
    {
        var l = context.GetChild(2).Accept(this).Single();
        var r = context.GetChild(4).Accept(this).Single();

        return [$"Expressions.VerticalDeviation({l}, {r})"];
    }

    #endregion
    
    #region Number binary operators

    public override List<string> VisitNumberMulDiv(Unipi.MppgParser.Grammar.MppgParser.NumberMulDivContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();
        var operation = context.op;

        switch (operation.Type)
        {
            case Unipi.MppgParser.Grammar.MppgParser.PROD_SIGN:
            {
                var firstType = context.GetChild(0).Accept(TypeVisitor);
                var secondType = context.GetChild(2).Accept(TypeVisitor);
        
                if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
                    return [$"{first}.Convolution({second})"];
                else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
                    return [$"{first} * {second}"];
                else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
                    return [$"{second} * {first}"];
                else
                    return [$"{first} * {second}"];
            }

            case Unipi.MppgParser.Grammar.MppgParser.DIV_SIGN:
            case Unipi.MppgParser.Grammar.MppgParser.DIV_OP:
            {
                var firstType = context.GetChild(0).Accept(TypeVisitor);
                var secondType = context.GetChild(2).Accept(TypeVisitor);
        
                if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
                    return [$"{first}.Deconvolution({second})"];
                else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
                    return [$"{first} / {second}"];
                else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
                    throw new InvalidOperationException($"Unexpected expression type: {context.GetJoinedText()}");
                else
                    return [$"{first} / {second}"];
            }
            
            default: 
                throw new InvalidOperationException($"Unexpected operation: {operation.Text}");
        }
    }

    public override List<string> VisitNumberSumSubMinMax(
        Unipi.MppgParser.Grammar.MppgParser.NumberSumSubMinMaxContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();
        var operation = context.op;

        switch (operation.Type)
        {
            case Unipi.MppgParser.Grammar.MppgParser.PLUS:
            {
                return [$"{first} + {second}"];
            }

            case Unipi.MppgParser.Grammar.MppgParser.MINUS:
            {
                return [$"{first} - {second}"];
            }

            case Unipi.MppgParser.Grammar.MppgParser.WEDGE:
            {
                var firstType = context.GetChild(0).Accept(TypeVisitor);
                var secondType = context.GetChild(2).Accept(TypeVisitor);
        
                if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
                    return [$"{first}.Minimum({second})"];
                else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
                {
                    var secondContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(0);
                    var secondWrapped = secondContext is not null ? WrapRationalExpressionIfNeeded(secondContext) : WrapRationalExpression(second);
                    return [$"{first}.Minimum(new Curve(new Sequence([ new Point(0, {secondWrapped}), Segment.Constant(0, 1, {secondWrapped})]), 0, 1, 0))"];
                }
                else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
                {
                    var firstContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(0);
                    var firstWrapped = firstContext is not null ? WrapRationalExpressionIfNeeded(firstContext) : WrapRationalExpression(first);
                    return [$"{second}.Minimum(new Curve(new Sequence([ new Point(0, {firstWrapped}), Segment.Constant(0, 1, {firstWrapped})]), 0, 1, 0))"];
                }
                else
                    return [$"RationalExpression.Min({first}, {second})"];
            }

            case Unipi.MppgParser.Grammar.MppgParser.VEE:
            {
                var firstType = context.GetChild(0).Accept(TypeVisitor);
                var secondType = context.GetChild(2).Accept(TypeVisitor);
        
                if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
                    return [$"{first}.Maximum({second})"];
                else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
                {
                    var secondContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(0);
                    var secondWrapped = secondContext is not null ? WrapRationalExpressionIfNeeded(secondContext) : WrapRationalExpression(second);
                    return [$"{first}.Maximum(new Curve(new Sequence([ new Point(0, {secondWrapped}), Segment.Constant(0, 1, {secondWrapped})]), 0, 1, 0))"];
                }
                else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
                {
                    var firstContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(0);
                    var firstWrapped = firstContext is not null ? WrapRationalExpressionIfNeeded(firstContext) : WrapRationalExpression(first);
                    return [$"{second}.Maximum(new Curve(new Sequence([ new Point(0, {firstWrapped}), Segment.Constant(0, 1, {firstWrapped})]), 0, 1, 0))"];
                }
                else
                    return [$"RationalExpression.Max({first}, {second})"];
            }

            default: throw new InvalidOperationException($"Unexpected operation: {operation.Text}");
        }
    }

    #endregion

    #region Utility

    private static List<string> CleanupReassignments(List<string> code)
    {
        var variableNames = code
            .Where(l => l.StartsWith("var "))
            .Select(l =>
            {
                var match = Regex.Match(l, @"^var (.+?) =");
                return match.Groups[1].Value;
            })
            .Distinct();

        var newCode = new List<string>(code);
        foreach (var name in variableNames)
        {
            var assignments = code
                .WithIndex()
                .Where(l => l.Item1.StartsWith($"var {name} ="))
                .ToList();
            if (assignments.Count > 1)
            {
                foreach (var (line, index) in assignments.Skip(1))
                {
                    newCode[index] = line.Replace($"var {name} = ", $"{name} = ");
                }
            }
        }

        return newCode;
    }

    #endregion
}
