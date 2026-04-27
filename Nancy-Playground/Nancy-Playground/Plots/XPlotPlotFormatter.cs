#if USE_XPLOT
using System.Diagnostics;
using Spectre.Console;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Playground.Cli.Nancy.Plots;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Utility;

namespace Unipi.Nancy.Playground.Cli.Plots;

/// <summary>
/// Implements plotting using <a href="https://github.com/fslaborg/XPlot">XPlot.Plotly</a>
/// Pros: renders an interactive plot on the browser. There, it is high-quality and explorable.
/// Cons: many issues when trying to export to image. Requires a browser to render, cannot run in headless environments.
/// Quality of the exported image is also poor.
/// </summary>
public class XPlotPlotFormatter: IPlotFormatter
{
    public string PlotsExportRoot { get; set; }

    public XPlotPlotFormatter(string? plotsRoot)
    {
        PlotsExportRoot = string.IsNullOrWhiteSpace(plotsRoot) ?
            Environment.CurrentDirectory :
            plotsRoot;
    }

    public void FormatPlot(PlotOutput plotOutput)
    {
        if (plotOutput.FunctionsToPlot.Count == 0)
            AnsiConsole.MarkupLine("[red]No functions to plot.[/]");
        else
        {
            var plotter = new PlotlyNancyPlotter();

            var curves = Enumerable
                .Select<(string Name, Curve Curve), Curve>(plotOutput.FunctionsToPlot, pair => pair.Curve)
                .ToList();
            var names = Enumerable
                .Select<(string Name, Curve Curve), string>(plotOutput.FunctionsToPlot, pair => pair.Name)
                .ToList();

            var plot = plotter.Plot(curves, names);

            var xlabel = string.IsNullOrWhiteSpace(plotOutput.XLabel) ?
                "x" : plotOutput.XLabel;
            var ylabel = string.IsNullOrWhiteSpace(plotOutput.YLabel) ?
                "y" : plotOutput.YLabel;

            plot.WithXTitle(xlabel);
            plot.WithYTitle(ylabel);

            // how to move the legend below?

            // default behavior: do open a browser tab to show the interactive plot
            var showInGui = plotOutput.Settings.ShowInGui ?? true;

            if (showInGui)
            {
                var html = plotter.GetHtml(plot);
                var htmlTempFileName = Path.GetTempPath() + Guid.NewGuid().ToString() + ".html";
                File.WriteAllText(htmlTempFileName, html);
                AnsiConsole.MarkupLine($"[gray]Html written to: {htmlTempFileName}; opening in default browser[/]");
                var psi = new ProcessStartInfo
                {
                    FileName = htmlTempFileName,
                    UseShellExecute = true
                };
                try {
                    Process.Start(psi);
                }
                catch(System.ComponentModel.Win32Exception)
                {
                    AnsiConsole.MarkupLine($"[yellow]Unable to open plot in browser.[/] [gray]Is this a container?[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine($"[gray]In-browser plot skipped.[/]");
            }

            if (!plotOutput.Settings.OutPath.IsNullOrWhiteSpace())
            {
                var imagePath = Path.Join(PlotsExportRoot, (string?)plotOutput.Settings.OutPath);
                byte[] imageBytes;
                try
                {
                    imageBytes = plotter.GetImage(plot);
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLine("Image rendering failed. May be due to dependencies: try running [yellow]nancy-playground setup[/]");
                    Console.WriteLine(e.Message);
                    return;
                }

                File.WriteAllBytes(imagePath, imageBytes);
            }
        }
    }
}
#endif