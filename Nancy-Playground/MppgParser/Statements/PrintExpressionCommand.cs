namespace Unipi.Nancy.Playground.MppgParser.Statements;

public record class PrintExpressionCommand : Statement
{
    public string VariableName { get; set; }

    public PrintExpressionCommand(string variableName)
    {
        VariableName = variableName;
    }

    public override string Execute(State state)
    {
        var (exists, type) = state.GetVariableType(VariableName);
        if(!exists)
            return $"ERROR: Variable \"{VariableName}\" not found";
        else
        {
            switch (type)
            {
                case ExpressionType.Function:
                {
                    var ce = state.GetFunctionVariable(VariableName);
                    return ce.ToUnicodeString();
                }
                case ExpressionType.Number:
                {
                    var ne = state.GetNumberVariable(VariableName);
                    return ne.ToUnicodeString();
                }
                default:
                {
                    return $"ERROR: Unknown expression type for variable \"{VariableName}\"";
                }
            }
        }
    }

    public override StatementOutput ExecuteToFormattable(State state)
    {
        var (exists, type) = state.GetVariableType(VariableName);
        if(!exists)
            throw new Exception($"Variable \"{VariableName}\" not found");
        else
        {
            string output;
            switch (type)
            {
                case ExpressionType.Function:
                {
                    var ce = state.GetFunctionVariable(VariableName);
                    output = ce.ToUnicodeString();
                    break;
                }
                case ExpressionType.Number:
                {
                    var ne = state.GetNumberVariable(VariableName);
                    output = ne.ToUnicodeString();
                    break;
                }
                default:
                {
                    throw new Exception($"Unknown expression type for variable \"{VariableName}\"");
                }
            }

            return new StatementOutput
            {
                StatementText = Text,
                OutputText = output
            };
        }
    }
}