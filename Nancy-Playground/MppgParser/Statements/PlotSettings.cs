using Unipi.Nancy.Numerics;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

public record PlotSettings
{
    /// <summary>
    /// The graph title.
    /// </summary>
    public ComputableString Title { get; init; } = new();

    /// <summary>
    /// Range for the x-axis.
    /// </summary>
    public (Rational Left, Rational Right)? XLimit { get; init; } = null;

    /// <summary>
    /// Range for the y-axis.
    /// </summary>
    public (Rational Left, Rational Right)? YLimit { get; init; } = null;

    /// <summary>
    /// Label for the x-axis.
    /// </summary>
    public ComputableString XLabel { get; init; } = new();

    /// <summary>
    /// Label for the y-axis.
    /// </summary>
    public ComputableString YLabel { get; init; } = new();

    /// <summary>
    /// Name of the png file to save the plot to.
    /// </summary>
    public string OutPath { get; init; } = string.Empty;

    /// <summary>
    /// If false, removes the grid from the plot.
    /// </summary>
    public bool ShowGrid { get; init; } = true;

    /// <summary>
    /// If false, remove the background from the plot.
    /// </summary>
    public bool ShowBackground { get; init; } = true;

    /// <summary>
    /// If false, the plot is NOT shown in a GUI.
    /// If true, the GUI used depends on the plot rendering used.
    /// </summary>
    public bool? ShowInGui { get; init; } = null;
}