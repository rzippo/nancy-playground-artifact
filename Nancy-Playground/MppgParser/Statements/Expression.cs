using Antlr4.Runtime;
using Unipi.Nancy.Expressions;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;
using Unipi.Nancy.Playground.MppgParser.Visitors;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

public class Expression
{
    private enum ExpressionSourceType
    {
        ExpressionContext,
        NancyExpression,
        VariableName
    }

    private ExpressionSourceType SourceType { get; init; }

    public ExpressionType ExpressionType =>
        NancyExpression switch
        {
            CurveExpression => ExpressionType.Function,
            RationalExpression => ExpressionType.Number,
            _ => ExpressionType.Undetermined
        };

    public IExpression? NancyExpression { get; internal set; }

    public Unipi.MppgParser.Grammar.MppgParser.ExpressionContext? ExpressionContext { get; private set; }

    public string? VariableName { get; private set; }

    public Expression(IExpression expression)
    {
        NancyExpression = expression;
        SourceType = ExpressionSourceType.NancyExpression;
    }

    public Expression(Unipi.MppgParser.Grammar.MppgParser.ExpressionContext context)
    {
        ExpressionContext = context;
        SourceType = ExpressionSourceType.ExpressionContext;
    }

    public Expression(string variableName)
    {
        VariableName = variableName;
        SourceType = ExpressionSourceType.VariableName;
    }

    public static Expression FromTree(Unipi.MppgParser.Grammar.MppgParser.ExpressionContext context, State? state)
    {
        var expression = ParseTree(context, state);
        return new Expression(expression);
    }

    public static IExpression ParseTree(Unipi.MppgParser.Grammar.MppgParser.ExpressionContext context, State? state)
    {
        var visitor = new ExpressionVisitor(state);
        var expression = visitor.Visit(context);
        return expression;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="state"></param>
    /// <returns>
    /// This method returns *either* a <see cref="CurveExpression"/>, if the expression resolves to a function,
    /// *or* a <see cref="RationalExpression"/> if the expression resolves to a number.
    /// The returned tuple will have null for the other type.  
    /// </returns>
    public static IExpression ParseFromString(string expression, State? state)
    {
        var inputStream = CharStreams.fromString(expression);
        var lexer = new Unipi.MppgParser.Grammar.MppgLexer(inputStream);
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new Unipi.MppgParser.Grammar.MppgParser(commonTokenStream);

        var context = parser.expression();
        var visitor = new ExpressionVisitor(state);

        return context.Accept(visitor);
    }

    public void ParseTree(State state)
    {
        switch (SourceType)
        {
            case ExpressionSourceType.ExpressionContext:
            {
                if(ExpressionContext == null)
                    throw new InvalidOperationException("Invalid state: no expression context");
                var expression = Expression.ParseTree(ExpressionContext, state);
                NancyExpression = expression;
                break;
            }
            case ExpressionSourceType.NancyExpression:
            {
                // do nothing
                break;
            }
            case ExpressionSourceType.VariableName:
            {
                if(string.IsNullOrWhiteSpace(VariableName))
                    throw new InvalidOperationException("Invalid state: no variable name");
                var expression = state.GetVariable(VariableName);
                NancyExpression = expression;
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public (Curve? function, Rational? number) Compute()
    {
        if (NancyExpression is CurveExpression ce)
            return (ce.Compute(), null);
        else if (NancyExpression is RationalExpression re)
            return (null, re.Compute());
        else
            throw new InvalidOperationException("No expression was parsed!");
    }
}