using System.Text;

namespace Unipi.Nancy.Playground.MppgParser.Utility;

public static class StringExtensions
{
    public static string TrimQuotes(this string str)
    {
        if(str.StartsWith('"'))
            str = str[1..];
        if (str.EndsWith('"'))
            str = str[0..^1];
        return str;
    }

    /// <inheritdoc cref="string.IsNullOrWhiteSpace"/>
    public static bool IsNullOrWhiteSpace(this string str)
    {
        return string.IsNullOrWhiteSpace(str);
    }

    /// <inheritdoc cref="string.IsNullOrEmpty"/>
    public static bool IsNullOrEmpty(this string str)
    {
        return string.IsNullOrEmpty(str);
    }

    /// <inheritdoc cref="string.Join(string?, IEnumerable{string})"/>
    public static string JoinText(this IEnumerable<string> strings, string? separator)
    {
        var sb = new StringBuilder();
        var first = true;
        foreach(var str in strings)
        {
            if(first)
                first = false;
            else if(separator != null)
                sb.Append(separator);
            sb.Append(str);
        }
        return sb.ToString();
    }

    /// <inheritdoc cref="string.Join(char, string?[])"/>
    public static string JoinText(this IEnumerable<string> strings, char separator = ' ')
    {
        var sb = new StringBuilder();
        var first = true;
        foreach(var str in strings)
        {
            if(first)
                first = false;
            else
                sb.Append(separator);
            sb.Append(str);
        }
        return sb.ToString();
    }
}