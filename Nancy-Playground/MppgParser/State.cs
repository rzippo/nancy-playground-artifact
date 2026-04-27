using Unipi.Nancy.Expressions;
using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser;

public class State
{
    private Dictionary<string, CurveExpression> FunctionVariables { get; init; } = new();
    private Dictionary<string, RationalExpression> NumberVariables { get; init; } = new();

    public State()
    {
    }

    public State(Dictionary<string, CurveExpression> functionVariables)
    {
        AddRange(functionVariables.Select(pair => (pair.Key, pair.Value)).ToList());
    }

    public State(Dictionary<string, RationalExpression> rationalExpressions)
    {
        AddRange(rationalExpressions.Select(pair => (pair.Key, pair.Value)).ToList());
    }

    public State(Dictionary<string, CurveExpression> functionVariables, Dictionary<string, RationalExpression> rationalExpressions)
    {
        AddRange(functionVariables.Select(pair => (pair.Key, pair.Value)).ToList());
        AddRange(rationalExpressions.Select(pair => (pair.Key, pair.Value)).ToList());
    }

    public State(List<(string Key, CurveExpression Value)> functionVariables)
    {
        AddRange(functionVariables);
    }

    public State(List<(string Key, RationalExpression Value)> rationalExpressions)
    {
        AddRange(rationalExpressions);
    }

    public State(List<(string Key, CurveExpression Value)> functionVariables, List<(string Key, RationalExpression Value)> rationalExpressions)
    {
        AddRange(functionVariables);
        AddRange(rationalExpressions);
    }

    public List<string> GetVariableNames()
    {
        var names = FunctionVariables.Keys
            .Concat(NumberVariables.Keys)
            .ToList();
        return names;
    }

    /// <summary>
    /// Determines the type of a variable based on the provided key.
    /// </summary>
    /// <param name="key">The key representing the variable to check.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    /// <item>
    /// <description>A <see cref="bool"/> indicating whether the variable exists as a function.</description>
    /// </item>
    /// <item>
    /// <description>A nullable <see cref="ExpressionType"/> indicating the type of the variable, or <c>null</c> if the variable does not exist.</description>
    /// </item>
    /// </list>
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the variable is present as both a function and a number.</exception>
    public (bool exists, ExpressionType? type) GetVariableType(string key)
    {
        var isFunction = FunctionVariables.ContainsKey(key);
        var isNumber = NumberVariables.ContainsKey(key);
        if (isFunction && isNumber)
            throw new InvalidOperationException($"The variabile {key} is present as BOTH function and number!");
        else if (isFunction)
            return (true, ExpressionType.Function);
        else if (isNumber)
            return (true, ExpressionType.Number);
        else
            return (false, null);
    }

    public CurveExpression GetFunctionVariable(string variableName)
    {
        return FunctionVariables[variableName];
    }

    public RationalExpression GetNumberVariable(string key)
    {
        return NumberVariables[key];
    }

    public IExpression GetVariable(string variableName)
    {
        var (exists, type) = GetVariableType(variableName);
        if(!exists)
            throw new ArgumentException($"The variable {variableName} is not present.");
        if(type == ExpressionType.Function)
            return FunctionVariables[variableName];
        else
            return NumberVariables[variableName];
    }

    public void StoreVariable(
        string name, 
        RationalExpression value, 
        bool overwrite = true, 
        bool changeType = false
    )
    {
        if (FunctionVariables.ContainsKey(name))
        {
            if (!overwrite && !changeType)
                throw new InvalidOperationException($"Variable {name} already exists as a function!");
            else
                FunctionVariables.Remove(name);
        }

        if (NumberVariables.ContainsKey(name) && !overwrite)
            throw new InvalidOperationException($"Variable {name} already exists!");
        else
            NumberVariables[name] = value.WithName(name);
    }

    public void StoreVariable(
        string name, 
        CurveExpression value, 
        bool overwrite = true, 
        bool changeType = false
    )
    {
        if(string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));

        if (NumberVariables.ContainsKey(name))
        {
            if (!overwrite && !changeType)
                throw new InvalidOperationException($"Variable {name} already exists as a number!");
            else
                NumberVariables.Remove(name);
        }

        if (FunctionVariables.ContainsKey(name) && !overwrite)
            throw new InvalidOperationException($"Variable {name} already exists!");
        else
            FunctionVariables[name] = value.WithName(name);
    }

    public void Add(string key, CurveExpression value)
        => StoreVariable(key, value, overwrite: false, changeType: false);

    public void Add(string key, RationalExpression value)
        => StoreVariable(key, value, overwrite: false, changeType: false);

    public void AddRange(List<(string key, CurveExpression value)> values)
        => values.ForEach(pair => Add(pair.key, pair.value));

    public void AddRange(List<(string key, RationalExpression value)> values)
        => values.ForEach(pair => Add(pair.key, pair.value));

    public void AddRange(Dictionary<string, CurveExpression> values)
    {
        foreach (var pair in values)
            Add(pair.Key, pair.Value);
    }

    public void AddRange(Dictionary<string, RationalExpression> values)
    {
        foreach (var pair in values)
            Add(pair.Key, pair.Value);
    }
}