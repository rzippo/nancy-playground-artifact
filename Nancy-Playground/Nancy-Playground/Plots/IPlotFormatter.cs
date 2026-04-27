using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.Cli.Plots;

public interface IPlotFormatter
{
    public string PlotsExportRoot { get; set; }

    public void FormatPlot(PlotOutput plotOutput);
}