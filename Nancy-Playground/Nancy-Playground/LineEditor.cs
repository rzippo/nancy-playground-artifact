using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Unipi.Nancy.Playground.Cli;

/// <summary>
/// Handles user input, with a navigable history of previous commands.
/// </summary>
[ExcludeFromCodeCoverage]
public class LineEditor
{
    /// <summary>
    /// History of entered commands.
    /// </summary>
    private readonly List<string> _history = new List<string>();
    /// <summary>
    /// Current index in the history for navigation.
    /// </summary>
    private int _historyIndex = -1;

    /// <summary>
    /// List of keywords for autocomplete.
    /// </summary>
    private readonly List<string> _keywords = [];
    /// <summary>
    /// Contextual keywords for autocomplete.
    /// </summary>
    private readonly List<ContextualKeywords> _contextualKeywords = [];
    /// <summary>
    /// List of session keywords for autocomplete.
    /// </summary>
    private readonly List<string> _sessionKeywords = [];

    public LineEditor()
    {
    }

    public LineEditor(
        IEnumerable<string> keywords, 
        IEnumerable<ContextualKeywords>? contextualKeywords = null
    )
    {
        _keywords.AddRange(keywords.Distinct());
        if (contextualKeywords != null)
            _contextualKeywords.AddRange(contextualKeywords.Distinct());
    }

    /// <summary>
    /// Replace the current keyword list with the given sequence.
    /// </summary>
    public void SetKeywords(IEnumerable<string> keywords)
    {
        _keywords.Clear();
        _keywords.AddRange(keywords.Distinct());
    }

    /// <summary>
    /// Replace the current contextual keyword list with the given sequence.
    /// </summary>
    /// <param name="contextualKeywords"></param>
    public void SetContextualKeywords(IEnumerable<ContextualKeywords> contextualKeywords)
    {
        _contextualKeywords.Clear();
        _contextualKeywords.AddRange(contextualKeywords.Distinct());
    }

    /// <summary>
    /// Add a single keyword to the autocomplete list.
    /// </summary>
    public void AddKeyword(string keyword)
    {
        if (!string.IsNullOrEmpty(keyword) && !_keywords.Contains(keyword))
            _keywords.Add(keyword);
    }

    /// <summary>
    /// Add contextual keywords to the autocomplete list.
    /// </summary>
    /// <param name="contextualKeywords"></param>
    public void AddContextualKeywords(ContextualKeywords contextualKeywords)
    {
        // todo: check for duplicates?
        _contextualKeywords.Add(contextualKeywords);
    }

    /// <summary>
    /// Add multiple keywords to the autocomplete list.
    /// </summary>
    public void AddKeywords(IEnumerable<string> keywords)
    {
        foreach (var k in keywords)
        {
            if (!string.IsNullOrEmpty(k) && !_keywords.Contains(k))
                _keywords.Add(k);
        }
    }

    /// <summary>
    /// Add multiple contextual keywords to the autocomplete list.
    /// </summary>
    /// <param name="contextualKeywords"></param>
    public void AddContextualKeywords(IEnumerable<ContextualKeywords> contextualKeywords)
    {
        // todo: check for duplicates?
        foreach (var ck in contextualKeywords)
            _contextualKeywords.Add(ck);
    }

    /// <summary>
    /// Replace the current session keyword list with the given sequence.
    /// </summary>
    public void SetSessionKeywords(IEnumerable<string> sessionKeywords)
    {
        _sessionKeywords.Clear();
        _sessionKeywords.AddRange(sessionKeywords.Distinct());
    }

    /// <summary>
    /// Reads a line from the console with command history support.
    /// Works similarly to Console.ReadLine(), but supports:
    /// - Up/Down arrow to navigate history
    /// - Left/Right, Home, End, Backspace, Delete
    /// - Ctrl+Left / Ctrl+Right to move by word
    /// </summary>
    public string ReadLine()
    {
        var buffer = new StringBuilder();
        // Cursor position. Does not count preamble.
        int cursor = 0;

        const string linePreamble = "> ";
        int linePreambleOffset = linePreamble.Length;
        int lineLeftBoundary = Console.CursorLeft;
        int cursorLeftBoundary = Console.CursorLeft + linePreambleOffset;

        // Starting cursor position (after your prompt)
        int startLeft = cursorLeftBoundary;
        int startTop = Console.CursorTop;

        // Characters previously printed out. Does not count preamble.
        int renderedLength = 0;

        List<string> completionMatches = [];
        int completionIndex = -1;
        int completionWordStart = 0;
        int completionWordLength = 0;

        // first render: empty prompt with preamble
        Render();

        while (true)
        {
            var keyInfo = Console.ReadKey(intercept: true);

            bool ctrl = (keyInfo.Modifiers & ConsoleModifiers.Control) != 0;
            bool isTab = keyInfo.Key == ConsoleKey.Tab;

            // Any non-Tab key resets the autocomplete cycling state
            if (!isTab)
            {
                completionMatches = [];
                completionIndex = -1;
            }

            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:
                    Console.SetCursorPosition(startLeft + buffer.Length, startTop);
                    Console.WriteLine(); // Move to next line

                    string line = buffer.ToString();

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        _history.Add(line);
                        _historyIndex = _history.Count; // one past last
                    }

                    return line;

                case ConsoleKey.Backspace:
                    if (cursor > 0)
                    {
                        buffer.Remove(cursor - 1, 1);
                        cursor--;
                        Render();
                    }
                    break;

                case ConsoleKey.Delete:
                    if (cursor < buffer.Length)
                    {
                        buffer.Remove(cursor, 1);
                        Render();
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    if (ctrl)
                    {
                        int newPos = FindPreviousWordStart(cursor);
                        if (newPos != cursor)
                        {
                            cursor = newPos;
                            Console.SetCursorPosition(startLeft + cursor, startTop);
                        }
                    }
                    else
                    {
                        if (cursor > 0)
                        {
                            cursor--;
                            Console.SetCursorPosition(startLeft + cursor, startTop);
                        }
                    }
                    break;

                case ConsoleKey.RightArrow:
                    if (ctrl)
                    {
                        int newPos = FindNextWordEnd(cursor);
                        if (newPos != cursor)
                        {
                            cursor = newPos;
                            Console.SetCursorPosition(startLeft + cursor, startTop);
                        }
                    }
                    else
                    {
                        if (cursor < buffer.Length)
                        {
                            cursor++;
                            Console.SetCursorPosition(startLeft + cursor, startTop);
                        }
                    }
                    break;

                case ConsoleKey.Home:
                    cursor = 0;
                    Console.SetCursorPosition(startLeft, startTop);
                    break;

                case ConsoleKey.End:
                    cursor = buffer.Length;
                    Console.SetCursorPosition(startLeft + cursor, startTop);
                    break;

                case ConsoleKey.UpArrow:
                    if (_history.Count > 0)
                    {
                        if (_historyIndex > 0)
                            _historyIndex--;
                        else
                            _historyIndex = 0;

                        buffer.Clear();
                        buffer.Append(_history[_historyIndex]);
                        cursor = buffer.Length;
                        Render();
                    }
                    break;

                case ConsoleKey.DownArrow:
                    if (_history.Count > 0)
                    {
                        if (_historyIndex < _history.Count - 1)
                        {
                            _historyIndex++;
                            buffer.Clear();
                            buffer.Append(_history[_historyIndex]);
                        }
                        else
                        {
                            // Past the end means "empty new line"
                            _historyIndex = _history.Count;
                            buffer.Clear();
                        }

                        cursor = buffer.Length;
                        Render();
                    }
                    break;

                case ConsoleKey.Tab:
                    {
                        // If we already have matches, cycle through them
                        if (completionMatches is { Count: > 0 })
                        {
                            completionIndex = (completionIndex + 1) % completionMatches.Count;
                        }
                        else
                        {
                            // First Tab press for this word: compute matches
                            int wordStart = FindPreviousWordStart(cursor);
                            int wordLen = cursor - wordStart;

                            if(wordLen <= 0)
                                break;
                            var activeKeywords = GetActiveKeywords(buffer.ToString(0, wordStart));

                            if (activeKeywords.Count == 0)
                                break;

                            string currentWord = buffer.ToString(wordStart, wordLen);

                            var matches = new List<string>();
                            foreach (var kw in activeKeywords)
                            {
                                if (kw.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                                {
                                    matches.Add(kw);
                                }
                            }

                            if (matches.Count == 0)
                                break;

                            completionMatches = matches;
                            completionIndex = 0;
                            completionWordStart = wordStart;
                            completionWordLength = wordLen;
                        }

                        // Apply the current completion
                        string completion = completionMatches[completionIndex];

                        buffer.Remove(completionWordStart, completionWordLength);
                        buffer.Insert(completionWordStart, completion);
                        completionWordLength = completion.Length;

                        cursor = completionWordStart + completionWordLength;
                        Render();
                    }
                    break;

                default:
                    char c = keyInfo.KeyChar;
                    if (c != '\0' && !char.IsControl(c))
                    {
                        buffer.Insert(cursor, c);
                        cursor++;
                        Render();
                    }
                    break;
            }
        }

        // local functions

        // Renders the current buffer and positions the cursor correctly
        void Render()
        {
            // Go back to where input starts
            Console.SetCursorPosition(lineLeftBoundary, startTop);

            Console.Write(linePreamble);
            string text = buffer.ToString();
            Console.Write(text);

            // Clear any leftover characters from previous render.
            // Both operands do not count preamble, so diff is coherent.
            int extra = renderedLength - text.Length;
            if (extra > 0)
            {
                Console.Write(new string(' ', extra));
            }

            renderedLength = text.Length;

            // Put cursor in correct position
            Console.SetCursorPosition(startLeft + cursor, startTop);
        }

        // Finds the start index of the previous word before the given position
        int FindPreviousWordStart(int position)
        {
            if (position <= 0 || buffer.Length == 0)
                return 0;

            int i = position - 1;

            // Skip non-word characters immediately to the left
            while (i >= 0 && !IsWordChar(buffer[i]))
                i--;

            // Move left until start of word
            while (i >= 0 && IsWordChar(buffer[i]))
                i--;

            return Math.Max(i + 1, 0);
        }

        // Finds the end index of the next word after the given position
        int FindNextWordEnd(int position)
        {
            int len = buffer.Length;
            if (position >= len || len == 0)
                return len;

            int i = position;

            // Skip non-word characters immediately to the right
            while (i < len && !IsWordChar(buffer[i]))
                i++;

            // Move right until end of word
            while (i < len && IsWordChar(buffer[i]))
                i++;

            return i;
        }

        // Determines if a character is considered part of a word
        bool IsWordChar(char c)
        {
            char[] punctuation = [ '.', ',', ';', ':', '?', '-', '(', ')', '[', ']', '{', '}', '<', '>', '/', '\\', '\'', '\"', '=' ];
            return !char.IsWhiteSpace(c) && !punctuation.Contains(c);
        }
    }

    /// <summary>
    /// Removes a history entry at the given index.
    /// Returns true if removed, false if index was invalid.
    /// </summary>
    public bool RemoveHistoryAt(int index)
    {
        if (index < 0 || index >= _history.Count)
            return false;

        _history.RemoveAt(index);

        // Reset history index to "one past the end"
        _historyIndex = _history.Count;
        return true;
    }

    /// <summary>
    /// Removes a history entry by value.
    /// If removeAll is false, removes only the first match.
    /// Returns true if at least one entry was removed.
    /// </summary>
    public bool RemoveHistory(string value, bool removeAll = false)
    {
        bool removed = false;
        if (removeAll)
        {
            for (int i = _history.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_history[i], value, StringComparison.Ordinal))
                {
                    _history.RemoveAt(i);
                    removed = true;
                }
            }
        }
        else
        {
            int idx = _history.FindIndex(h => string.Equals(h, value, StringComparison.Ordinal));
            if (idx >= 0)
            {
                _history.RemoveAt(idx);
                removed = true;
            }
        }

        if (removed)
        {
            _historyIndex = _history.Count;
        }

        return removed;
    }

    /// <summary>
    /// Adds one or more lines to the history.
    /// </summary>
    /// <param name="lines">The lines to add to history</param>
    public void AddToHistory(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                _history.Add(line);
            }
        }
        _historyIndex = _history.Count; // one past last
    }

    /// <summary>
    /// Clears all command history.
    /// </summary>
    public void ClearHistory()
    {
        _history.Clear();
        _historyIndex = 0;
    }

    /// <summary>
    /// Gets the list of active keywords for autocomplete, given the current line before the cursor.
    /// </summary>
    /// <param name="currentLineBeforeCursor"></param>
    /// <returns></returns>
    private List<string> GetActiveKeywords(string currentLineBeforeCursor)
    {
        var result = new List<string>();
        // Always-available keywords
        result.AddRange(_keywords);
        result.AddRange(_sessionKeywords);

        if (_contextualKeywords.Count == 0 || string.IsNullOrWhiteSpace(currentLineBeforeCursor))
            return result;

        // For each contextual keyword, check if any enabler token appeared earlier in this line
        foreach (var ck in _contextualKeywords)
        {
            bool enabled = false;

            foreach (var enabler in ck.Enablers)
            {
                if (currentLineBeforeCursor.Contains(enabler, StringComparison.OrdinalIgnoreCase))
                {
                    enabled = true;
                    break;
                }
                if (enabled) break;
            }

            if (enabled)
            {
                result.AddRange(ck.Keywords);
            }
        }

        return result;
    }
}

[ExcludeFromCodeCoverage]
public record ContextualKeywords
{
    public List<string> Enablers { get; init; } = [];
    public List<string> Keywords { get; init; } = [];
}