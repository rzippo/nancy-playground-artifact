using System.Globalization;
using System.Runtime.CompilerServices;

namespace Unipi.Nancy.Playground.Cli.Utility;

public static class FormattableStringExtensions
{
    /// <summary>
    /// Concatenates two FormattableStrings.
    /// </summary>
    public static FormattableString Concat(this FormattableString first, FormattableString second)
    {
        var firstArgs = first.GetArguments();
        var secondArgs = second.GetArguments();

        // Renumber placeholders in the second format string
        string adjustedSecondFormat = string.Format(
            CultureInfo.InvariantCulture,
            second.Format,
            Enumerable.Range(0, secondArgs.Length)
                .Select(i => "{" + (i + firstArgs.Length) + "}")
                .ToArray<object?>()
        );

        var combinedFormat = first.Format + adjustedSecondFormat;
        var combinedArgs = firstArgs.Concat(secondArgs).ToArray();

        return FormattableStringFactory.Create(combinedFormat, combinedArgs);
    }
}