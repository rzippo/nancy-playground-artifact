using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

public class CommentParsing
{
    private readonly ITestOutputHelper _testOutputHelper;

    public CommentParsing(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static List<(string commentLine, string expectedText)> KnownCommentPairs =
    [
        ("// This is a comment", "// This is a comment"),
        ("// comment", "// comment"),
        ("// comment # recomment", "// comment # recomment"),
        ("# This is a comment", "# This is a comment"),
        ("# comment", "# comment"),
        ("## This is a comment", "## This is a comment"),
        ("### This is a comment", "### This is a comment"),
        ("% This is a comment", "% This is a comment"),
        ("% comment", "% comment"),
        ("> This is a comment", "> This is a comment"),
        ("> comment", "> comment"),
        ("# This ' is a : comment with ! weird < chars > inside '", "# This ' is a : comment with ! weird < chars > inside '"),
        ("// This ' is a : comment with ! weird < chars > inside '", "// This ' is a : comment with ! weird < chars > inside '"),
        ("# This ' is a : comment with ! weird < chars > inside ’", "# This ' is a : comment with ! weird < chars > inside ’"),
        ("// This ' is a : comment with ! weird < chars > inside ’", "// This ' is a : comment with ! weird < chars > inside ’"),
    ];

    public static IEnumerable<object[]> KnownCommentTestCases =>
        KnownCommentPairs.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(KnownCommentTestCases))]
    public void CommentParsingEquivalence(string commentLine, string expectedText)
    {
        var statement = Statement.FromLine(commentLine);
        Assert.IsAssignableFrom<Comment>(statement);
        var comment = (Comment)statement;
        Assert.Equal(expectedText, comment.Text);
    }
}
