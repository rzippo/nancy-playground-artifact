using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Expressions;
using Unipi.Nancy.Playground.MppgParser.Exceptions;
using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

public partial class ExpressionVisitor : MppgBaseVisitor<IExpression>
{
    public State State { get; init; }

    public ExpressionVisitor(State? state)
    {
        State = state ?? new();
    }

    public override IExpression VisitFunctionVariableExp(Unipi.MppgParser.Grammar.MppgParser.FunctionVariableExpContext context)
    {
        var name = context.GetText();
        var (isPresent, type) = State.GetVariableType(name);
        if (!isPresent || type is null)
            throw new VariableNotFoundException($"Variable '{name}' not found");
        if (type == ExpressionType.Function)
            return State.GetFunctionVariable(name);
        else
            return State.GetNumberVariable(name);
    }

    public override IExpression VisitNumberVariableExp(Unipi.MppgParser.Grammar.MppgParser.NumberVariableExpContext context)
    {
        var name = context.GetText();
        var (isPresent, type) = State.GetVariableType(name);
        if (!isPresent || type is null)
            throw new VariableNotFoundException($"Variable '{name}' not found");
        if (type == ExpressionType.Function)
            return State.GetFunctionVariable(name);
        else
            return State.GetNumberVariable(name);
    }

    public override IExpression VisitEncNumberVariableExp(Unipi.MppgParser.Grammar.MppgParser.EncNumberVariableExpContext context)
    {
        var name = context.GetText();
        var (isPresent, type) = State.GetVariableType(name);
        if (!isPresent || type is null)
            throw new VariableNotFoundException($"Variable '{name}' not found");
        if (type == ExpressionType.Function)
            return State.GetFunctionVariable(name);
        else
            return State.GetNumberVariable(name);
    }

    public override IExpression VisitNumberLiteralExp(Unipi.MppgParser.Grammar.MppgParser.NumberLiteralExpContext context)
    {
        var numberLiteralVisitor = new NumberLiteralVisitor();
        var value = numberLiteralVisitor.Visit(context);

        var valueExp = Expressions.Expressions.FromRational(value, "");
        return valueExp;
    }

    public override IExpression VisitEncNumberLiteralExp(Unipi.MppgParser.Grammar.MppgParser.EncNumberLiteralExpContext context)
    {
        var numberLiteralVisitor = new NumberLiteralVisitor();
        var value = numberLiteralVisitor.Visit(context);

        var valueExp = Expressions.Expressions.FromRational(value, "");
        return valueExp;
    }
}