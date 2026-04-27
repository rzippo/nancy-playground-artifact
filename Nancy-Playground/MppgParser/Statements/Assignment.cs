using System.Diagnostics;
using Unipi.Nancy.Expressions;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

public record class Assignment : Statement
{
    public string VariableName { get; set; }
    public Expression Expression { get; set; }

    public Assignment(string variableName, Expression expression)
    {
        VariableName = variableName;
        Expression = expression;
    }

    public override string Execute(State state)
        => Execute(state, false, true, false);

    public string Execute(
        State state,
        bool computeValue,
        bool overwrite = true, 
        bool changeType = false
    )
    {
        try
        {
            Expression.ParseTree(state);
            switch (Expression.NancyExpression)
            {
                case CurveExpression ce:
                {
                    if(computeValue) 
                        ce.Compute();
                    state.StoreVariable(VariableName, ce, overwrite, changeType);
                    break;
                }
                case RationalExpression re:
                {
                    if(computeValue) 
                        re.Compute();
                    state.StoreVariable(VariableName, re, overwrite, changeType);
                    break;
                }
                default:
                    throw new Exception($"Expression could not be parsed");
            }

            return VariableName;
        }
        catch (Exception e)
        {
            return e.Message;   
        }
    }

    public override StatementOutput ExecuteToFormattable(State state)
        => ExecuteToFormattable(state, false, true, false);

    public AssignmentOutput ExecuteToFormattable(
        State state,
        bool immediateComputeValue,
        bool overwrite = true, 
        bool changeType = false
    )
    {
        var sw = Stopwatch.StartNew();
        Expression.ParseTree(state);
        switch (Expression.NancyExpression)
        {
            case CurveExpression ce:
            {
                if(immediateComputeValue)
                    ce.Compute();
                state.StoreVariable(VariableName, ce, overwrite, changeType);
                break;
            }
            case RationalExpression re:
            {
                if(immediateComputeValue)
                    re.Compute();
                state.StoreVariable(VariableName, re, overwrite, changeType);
                break;
            }
            default:
                throw new Exception($"Expression could not be parsed");
        }
        sw.Stop();

        return new AssignmentOutput
        {
            StatementText = Text,
            OutputText = VariableName,
            AssignedVariable = VariableName,
            Expression = Expression.NancyExpression,
            Time = sw.Elapsed,
        };
    }
}