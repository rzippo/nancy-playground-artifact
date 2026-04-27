using System.Text;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

public class ComputableString
{
    internal List<object> Pieces { get; } = [];

    public void Append(string s)
    {
        Pieces.Add(s);
    }

    public void Append(Expression e)
    {
        Pieces.Add(e);
    }

    public void Concat(ComputableString cs)
    {
        Pieces.AddRange(cs.Pieces);
    }

    public string Compute(State state)
    {
        var sb = new StringBuilder();
        foreach (var piece in Pieces)
        {
            if(piece is string s)
                sb.Append(s);
            else if (piece is Expression expression)
            {
                expression.ParseTree(state);
                var (c, r) = expression.Compute();
                if(c is not null)
                    sb.Append(c);
                else if (r is not null)
                    sb.Append(r);
            }
        }
        return sb.ToString();
    }
}
