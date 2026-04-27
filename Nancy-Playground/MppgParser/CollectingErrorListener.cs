using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Unipi.Nancy.Playground.MppgParser;

public sealed record SyntaxErrorInfo(
    int Line,
    int Column,
    string Message,

    SyntaxErrorInfo.ErrorType Type,
    
    // Offending token / character
    string? OffendingText,
    int? OffendingTokenType,

    // Parser context (null for lexer errors)
    string? RuleName,
    IReadOnlyList<string>? RuleStack,

    // Expected tokens (parser only)
    IReadOnlyList<string>? Expected,

    // Nice-to-have: a small source excerpt
    string? SourceExcerpt
)
{
    public enum ErrorType { Lexer, Parser}

    public override string ToString()
        => ToString(false);
    public string ToString(bool verbose)
    {
        if (verbose)
        {
            return $"line {Line}:{Column} {Message}" +
                   (RuleName != null ? $" [rule: {RuleName}]" : "") +
                   (Expected != null && Expected.Count > 0
                       ? $" expected: {string.Join(", ", Expected)}"
                       : "");
        }
        else
        {
            return $"line {Line}:{Column} {Message}";
        }
    }
};
public sealed class DiagnosticLexerErrorListener : IAntlrErrorListener<int>
{
    private readonly IList<SyntaxErrorInfo> _errors;
    private readonly ICharStream? _charStream;
    private readonly int _excerptRadius;

    public DiagnosticLexerErrorListener(IList<SyntaxErrorInfo> errors, ICharStream? charStream, int excerptRadius = 25)
    {
        _errors = errors ?? throw new ArgumentNullException(nameof(errors));
        _charStream = charStream;
        _excerptRadius = excerptRadius;
    }
    public void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        int offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        string? offendingText = null;
        if (offendingSymbol >= 0)
        {
            // offendingSymbol is a Unicode code point
            offendingText = char.ConvertFromUtf32(offendingSymbol);
        }
        
        // In the lexer callback we don't reliably get the absolute char index,
        // so excerpt is best-effort based on (line,column).
        // If you want perfect lexer excerpts, you typically override Lexer.NotifyListeners.
        var excerpt = TryGetExcerptFromLineColumn(_charStream, line, charPositionInLine, _excerptRadius);

        _errors.Add(new SyntaxErrorInfo(
            Line: line,
            Column: charPositionInLine,
            Message: msg,
            Type: SyntaxErrorInfo.ErrorType.Lexer,
            OffendingText: offendingText,
            OffendingTokenType: null,
            RuleName: null,
            RuleStack: null,
            Expected: null,
            SourceExcerpt: excerpt
        ));
    }
    
    private static string? TryGetExcerptFromLineColumn(ICharStream? input, int line1Based, int col0Based, int radius)
    {
        if (input == null) return null;

        // Convert to absolute index by scanning text for line breaks.
        // This is O(n) per error, but lexer errors should be rare.
        var text = input.GetText(new Interval(0, input.Size - 1));
        if (string.IsNullOrEmpty(text)) return null;

        int targetLine = Math.Max(1, line1Based);
        int line = 1;
        int i = 0;

        while (i < text.Length && line < targetLine)
        {
            if (text[i] == '\n') line++;
            i++;
        }

        int absIndex = i + Math.Max(0, col0Based);
        absIndex = Math.Clamp(absIndex, 0, Math.Max(0, text.Length - 1));

        int start = Math.Max(0, absIndex - radius);
        int stop = Math.Min(text.Length - 1, absIndex + radius);

        var excerpt = text.Substring(start, stop - start + 1)
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");

        return excerpt;
    }
}

public sealed class DiagnosticParserErrorListener : BaseErrorListener
{
    private readonly IList<SyntaxErrorInfo> _errors;
    private readonly int _excerptRadius;


    public DiagnosticParserErrorListener(IList<SyntaxErrorInfo> errors, int excerptRadius = 25)
    {
        _errors = errors ?? throw new ArgumentNullException(nameof(errors));
        _excerptRadius = excerptRadius;
    }
    public override void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        string? ruleName = null;
        IReadOnlyList<string>? ruleStack = null;
        IReadOnlyList<string>? expected = null;

        if (recognizer is Parser parser)
        {
            var ctx = parser.Context;
            if (ctx != null)
            {
                ruleName = SafeRuleName(parser, ctx.RuleIndex);
                ruleStack = GetRuleStack(parser, ctx);
            }

            expected = GetExpectedTokenNames(parser);
        }

        var excerpt = TryGetExcerptFromToken(offendingSymbol, _excerptRadius);

        _errors.Add(new SyntaxErrorInfo(
            Line: line,
            Column: charPositionInLine,
            Message: msg,
            Type: SyntaxErrorInfo.ErrorType.Parser,
            OffendingText: offendingSymbol?.Text,
            OffendingTokenType: offendingSymbol?.Type,
            RuleName: ruleName,
            RuleStack: ruleStack,
            Expected: expected,
            SourceExcerpt: excerpt
        ));
    }
    
    private static string? SafeRuleName(Parser parser, int ruleIndex)
    {
        if (parser.RuleNames == null) return null;
        return (ruleIndex >= 0 && ruleIndex < parser.RuleNames.Length) ? parser.RuleNames[ruleIndex] : null;
    }

    private static IReadOnlyList<string> GetRuleStack(Parser parser, ParserRuleContext ctx)
    {
        var stack = new List<string>();
        for (ParserRuleContext? c = ctx; c != null; c = c.Parent as ParserRuleContext)
        {
            var name = SafeRuleName(parser, c.RuleIndex);
            stack.Add(name ?? c.RuleIndex.ToString());
        }
        stack.Reverse(); // outermost -> innermost
        return stack;
    }

    private static IReadOnlyList<string> GetExpectedTokenNames(Parser parser)
    {
        var vocab = parser.Vocabulary;
        var set = parser.GetExpectedTokens(); // IntervalSet

        var names = new List<string>();
        foreach (var t in set.ToArray())
        {
            // Prefer literal name (e.g. "')'"), then symbolic (e.g. IDENT), else number
            names.Add(vocab.GetLiteralName(t) ?? vocab.GetSymbolicName(t) ?? t.ToString());
        }
        return names;
    }

    private static string? TryGetExcerptFromToken(IToken? token, int radius)
    {
        if (token == null) return null;

        // For parser errors, TokenSource.InputStream is typically the original ICharStream.
        if (token.TokenSource?.InputStream is not { } input) return null;

        // Prefer StartIndex when valid; fall back to stream index.
        int idx = token.StartIndex >= 0 ? token.StartIndex : input.Index;
        idx = Math.Clamp(idx, 0, Math.Max(0, input.Size - 1));

        int start = Math.Max(0, idx - radius);
        int stop = Math.Min(input.Size - 1, idx + radius);
        if (stop < start) return null;

        return input.GetText(new Interval(start, stop))
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }
}
