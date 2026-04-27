using Antlr4.Runtime.Tree;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

public static class IParseTreeExtensions
{
    public static List<string> GetChildText(this IParseTree tree)
    {
        if (tree.ChildCount == 0)
        {
            return [ tree.GetText() ];
        }
        else
        {
            var result = new List<string>();
            for (int i = 0; i < tree.ChildCount; i++)
            {
                var child = tree.GetChild(i);
                result.AddRange(child.GetChildText());
            }
            return result;
        }
    }

    public static string GetJoinedText(this IParseTree tree, string separator = " ")
    {
        return string.Join(separator, GetChildText(tree));
    }
}