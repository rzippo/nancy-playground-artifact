namespace Unipi.Nancy.Playground.Cli;

public partial class InteractiveCommand
{
    private static List<string> Keywords =
    [
        // higher-order commands
        "!help",
        "!clihelp",
        "!quit",
        "!exit",
        "!export",
        "!save",
        "!convert",
        "!load",
        "!clear",
        // curves
        "ratency",
        "bucket",
        "affine",
        "step",
        "stair",
        "delay",
        "zero",
        "epsilon",
        "upp",
        "uaf",
        // operations
        "star",
        "hShift",
        "vShift",
        "inv",
        "low_inv",
        "up_inv",
        "upclosure",
        "nnupclosure",
        "comp",
        "left-ext",
        "right-ext",
        "hDev",
        "vDev",
        "zDev",
        // "maxBacklogPeriod", not implemented yet
        "plot",
        "assert",
        "printExpression"
    ];

    private static List<ContextualKeywords> ContextualKeywords() =>
    [
        new ContextualKeywords
        {
            Enablers = ["upp"],
            Keywords =
            [
                "period",
            ]
        },
        new ContextualKeywords
        {
            Enablers = ["plot"],
            Keywords =
            [
                "main",
                "title",
                "xlim",
                "ylim",
                "xlab",
                "ylab",
                "out",
                "grid",
                "bg",
                "gui",
            ]
        },
        new ContextualKeywords
        {
            Enablers = ["!help"],
            Keywords = NancyPlaygroundDocs.HelpDocument
                .Sections
                .SelectMany(section => section.Tags)
                .Concat(
                    NancyPlaygroundDocs.HelpDocument
                        .Sections
                        .SelectMany(section => section.Items)
                        .SelectMany(item => item.Tags)
                )
                .Distinct()
                .ToList()
        }
    ];
}