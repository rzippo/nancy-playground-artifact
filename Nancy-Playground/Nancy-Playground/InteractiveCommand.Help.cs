using System.Text.RegularExpressions;
using Spectre.Console;

using Unipi.Nancy.Playground.MppgParser.Utility;

namespace Unipi.Nancy.Playground.Cli;

public partial class InteractiveCommand
{
    private void PrintHelp(string[] args)
    {
        if (args.Length > 0)
            PrintSearchLong(NancyPlaygroundDocs.HelpDocument, args);
        else
            PrintShort(NancyPlaygroundDocs.HelpDocument);
    }

    /// <summary>
    /// Prints all sections and items of a HelpDocument in a short, colored form.
    /// </summary>
    public static void PrintShort(HelpDocument doc)
    {
        if (!string.IsNullOrWhiteSpace(doc.Preamble))
        {
            AnsiConsole.MarkupLine($"[grey]{Escape(doc.Preamble.Trim())}[/]");
            AnsiConsole.WriteLine();
        }

        foreach (var section in doc.Sections)
        {
            PrintSectionShort(section);
            AnsiConsole.WriteLine();
        }
    }

    public static void PrintSearchLong(HelpDocument doc, IReadOnlyList<string> args)
    {
        static string NormalizeQuery(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return string.Empty;
            var t = s.Trim().ToLowerInvariant();
            t = t.Replace("_", "-");
            if (t.Length > 1 && t.EndsWith("s"))
                t = t[..^1];
            return t;
        }

        var userQuery = NormalizeQuery(args[0]);

        var searchMatches = NancyPlaygroundDocs.HelpDocument.Sections
            .Where(section =>
                section.Tags.Any(tag => Regex.IsMatch(tag, userQuery)) ||
                section.Items.Any(item =>
                    item.Tags.Any(tag => Regex.IsMatch(tag, userQuery))
                )
            )
            .Select(section =>
                {
                    if (section.Tags.Any(tag => Regex.IsMatch(tag, userQuery)))
                        return section;
                    else
                    {
                        var filteredSection = section with
                        {
                            Items = section.Items
                                .Where(item => item.Tags.Any(tag => Regex.IsMatch(tag, userQuery)))
                                .ToList()
                        };
                        return filteredSection;
                    }
                }
            )
            .ToList();

        if (searchMatches.Any())
        {
            // if (!string.IsNullOrWhiteSpace(doc.Preamble))
            // {
            //     AnsiConsole.MarkupLine($"[grey]{Escape(doc.Preamble.Trim())}[/]");
            //     AnsiConsole.WriteLine();
            // }
            foreach (var section in searchMatches)
                PrintSectionLong(section);
        }
        else
            AnsiConsole.MarkupLine($"[yellow]No match found for the given keywords.[/]");
    }

    private static void PrintSectionShort(HelpSection section)
    {
        var tagText = section.Tags is { Count: > 0 }
            ? $" [grey]({Escape(string.Join(", ", section.Tags))})[/]"
            : string.Empty;

        // AnsiConsole.MarkupLine($"[bold yellow]{Escape(section.Name)}[/]{tagText}");
        AnsiConsole.MarkupLine($"[bold yellow]{Escape(section.Name)}[/]");

        if (!string.IsNullOrWhiteSpace(section.Description))
        {
            AnsiConsole.MarkupLine($"[dim]{Escape(section.Description)}[/]");
        }

        foreach (var item in section.Items)
        {
            PrintItemShort(item);
        }
    }

    private static void PrintItemShort(HelpItem item)
    {
        var tagText = item.Tags is { Count: > 0 }
            ? $" [grey]({Escape(string.Join(", ", item.Tags))})[/]"
            : string.Empty;

        // Item name + optional tags
        // AnsiConsole.MarkupLine($"  [cyan]- {Escape(item.Name)}[/]{tagText}");
        AnsiConsole.MarkupLine($"  [cyan]- {Escape(item.Name)}[/] {MarkupFormats(item.Formats)}");

        // One-line short description (truncated)
        if (!string.IsNullOrWhiteSpace(item.Description))
        {
            // var shortDesc = TruncateSingleLine(item.Description, 80);
            // AnsiConsole.MarkupLine($"    [dim]{Escape(shortDesc)}[/]");
            AnsiConsole.MarkupLine($"    [dim]{Escape(item.Description)}[/]");
        }
    }

    private static void PrintSectionLong(HelpSection section)
    {
        AnsiConsole.MarkupLine($"[bold yellow]{Escape(section.Name)}[/]");

        // does printing tags make sense? Make it an option?
        // var tagText = section.Tags is { Count: > 0 }
        //     ? $" [grey]({Escape(string.Join(", ", section.Tags))})[/]"
        //     : string.Empty;
        // AnsiConsole.MarkupLine(tagText);

        if (!string.IsNullOrWhiteSpace(section.Description))
            AnsiConsole.MarkupLine($"[dim]{Escape(section.Description)}[/]");

        foreach (var item in section.Items)
            PrintItemLong(item);
    }

    private static void PrintItemLong(HelpItem item)
    {
        AnsiConsole.MarkupLine($"  [cyan]- {Escape(item.Name)}[/] {MarkupFormats(item.Formats)}");

        // does printing tags make sense? Make it an option?

        // var tagText = item.Tags is { Count: > 0 }
        //     ? $" [grey]({Escape(string.Join(", ", item.Tags))})[/]"
        //     : string.Empty;
        // AnsiConsole.MarkupLine(tagText);

        var description = item.LongDescription.IsNullOrWhiteSpace() ? item.Description : item.LongDescription;
        var descriptionLines = description.Split("\n");
        foreach (var descriptionLine in descriptionLines)
            AnsiConsole.MarkupLine($"    [dim]{Escape(descriptionLine)}[/]");
    }

    private static string TruncateSingleLine(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var oneLine = text.Replace("\r", " ").Replace("\n", " ").Trim();
        if (oneLine.Length <= maxLength)
            return oneLine;

        return oneLine[..(maxLength - 3)] + "...";
    }

    /// <summary>
    /// Escapes Spectre.Console markup special characters.
    /// </summary>
    private static string Escape(string text)
    {
        return Markup.Escape(text ?? string.Empty);
    }

    private static string MarkupFormats(List<string> formats)
    {
        return formats
            .Select(format => $"[green]{Escape(format)}[/]")
            .JoinText(" [dim]|[/] ");
    }
}