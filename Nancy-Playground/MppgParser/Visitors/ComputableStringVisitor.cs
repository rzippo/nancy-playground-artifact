using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Utility;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

public class ComputableStringVisitor : MppgBaseVisitor<ComputableString>
{
    public override ComputableString VisitString(Unipi.MppgParser.Grammar.MppgParser.StringContext context)
    {
        var cs = new ComputableString();
        for (int i = 0; i < context.ChildCount; i++)
        {
            var child = context.GetChild(i);
            var ics = Visit(child);
            if(ics != null)
                cs.Concat(ics);
        }

        return cs;
    }

    public override ComputableString VisitStringLiteral(Unipi.MppgParser.Grammar.MppgParser.StringLiteralContext context)
    {
        var cs = new ComputableString();
        var str = context.GetText().TrimQuotes();
        cs.Append(str);
        return cs;
    }

    public override ComputableString VisitStringVariable(Unipi.MppgParser.Grammar.MppgParser.StringVariableContext context)
    {
        var cs = new ComputableString();
        var name = context.GetText();
        var expression = new Expression(name);
        cs.Append(expression);
        return cs;
    }

    public override ComputableString VisitNumberLiteral(Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext context)
    {
        var cs = new ComputableString();
        var visitor = new NumberLiteralVisitor();
        var number = visitor.Visit(context);
        cs.Append(number.ToString());
        return cs;
    }
}