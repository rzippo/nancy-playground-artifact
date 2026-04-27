using Unipi.Nancy.Expressions;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

public record class PlotCommand : Statement
{
    public List<Expression> FunctionsToPlot { get; init; } = [];
    public PlotSettings Settings { get; init; } = new();
    
    public override string Execute(State state)
    {
        return "Plotting is not implemented in this context.";
    }

    public override StatementOutput ExecuteToFormattable(State state)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var functions = FunctionsToPlot
            .Select(ex =>
            {   
                ex.ParseTree(state);
                if(ex.NancyExpression is CurveExpression ce)
                    return (ce.Name, ce.Compute());
                else
                    throw new Exception("Cannot plot a number.");
            })
            .ToList();
        var title = Settings.Title.Compute(state);
        var xLabel = Settings.XLabel.Compute(state);
        var yLabel = Settings.YLabel.Compute(state);

        stopwatch.Stop();

        return new PlotOutput
        {
            FunctionsToPlot = functions,
            Title = title,
            XLabel = xLabel,
            YLabel = yLabel,
            Settings = Settings,
            Time = stopwatch.Elapsed,
            StatementText = Text,
            OutputText = "If you are reading this, the formatter does not implement plots."
        };
    }
}