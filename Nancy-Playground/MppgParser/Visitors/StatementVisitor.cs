using Antlr4.Runtime.Misc;
using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Utility;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

public class StatementVisitor : MppgBaseVisitor<Statement>
{
    public override Statement VisitStatementLine([NotNull] Unipi.MppgParser.Grammar.MppgParser.StatementLineContext context)
    {
        var statementContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.StatementContext>(0);
        var inlineCommentContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.InlineCommentContext>(0);

        var statement = statementContext.Accept(this);
        if (inlineCommentContext != null)
        {
            var inlineComment = inlineCommentContext.GetJoinedText();
            if (statement is Comment c)
                return c with
                {
                    Text = $"{c.Text} {inlineComment}"
                };
            else
                return statement with
                {
                    InlineComment = inlineComment
                };
        }
        else
            return statement;
    }

    public override Statement VisitComment(Unipi.MppgParser.Grammar.MppgParser.CommentContext context)
    {
        var text = context.GetJoinedText();
        return new Comment { Text = text };
    }

    public override Statement VisitEmpty([NotNull] Unipi.MppgParser.Grammar.MppgParser.EmptyContext context)
    {
        return new EmptyStatement();
    }

    public override Statement VisitPlotCommand(Unipi.MppgParser.Grammar.MppgParser.PlotCommandContext context)
    {
        var text = context.GetJoinedText();
        var args = context.GetRuleContexts<Unipi.MppgParser.Grammar.MppgParser.PlotArgContext>();

        var functionNameContexts = args
            .Select(arg => arg.GetChild<Unipi.MppgParser.Grammar.MppgParser.FunctionNameContext>(0))
            .Where(ctx => ctx != null);
        var plotOptionContexts = args
            .Select(arg => arg.GetChild<Unipi.MppgParser.Grammar.MppgParser.PlotOptionContext>(0))
            .Where(ctx => ctx != null);
        
        var variableNames = functionNameContexts
            .Select(ctx => ctx.GetText())
            .Select(name => new Expression(name))
            .ToList();

        var settings = new PlotSettings();
        foreach (var plotArgContext in plotOptionContexts)
        {
            var argName = plotArgContext.GetChild(0).GetText();
            var argString = plotArgContext.GetChild(2).GetText()
                .TrimQuotes();

            switch (argName)
            {
                case "main":
                {
                    var stringContext = plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.StringContext>(0);
                    var visitor = new ComputableStringVisitor();
                    var cs = visitor.Visit(stringContext);
                    settings = settings with
                    {
                        Title = cs
                    };
                    break;
                }

                case "title":
                {
                    var stringContext = plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.StringContext>(0);
                    var visitor = new ComputableStringVisitor();
                    var cs = visitor.Visit(stringContext);
                    settings = settings with
                    {
                        Title = cs
                    };
                    break;
                }

                case "xlim":
                {
                    var intervalContext = plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.IntervalContext>(0);
                    var numberVisitor = new NumberLiteralVisitor();
                    var leftLimitContext = intervalContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext>(0);
                    var rightLimitContext = intervalContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext>(1);
                    var leftLimit = numberVisitor.Visit(leftLimitContext);
                    var rightLimit = numberVisitor.Visit(rightLimitContext);
                    settings = settings with
                    {
                        XLimit = (leftLimit, rightLimit)
                    };
                    break;
                }

                case "ylim":
                {
                    var intervalContext = plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.IntervalContext>(0);
                    var numberVisitor = new NumberLiteralVisitor();
                    var leftLimitContext = intervalContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext>(0);
                    var rightLimitContext = intervalContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext>(1);
                    var leftLimit = numberVisitor.Visit(leftLimitContext);
                    var rightLimit = numberVisitor.Visit(rightLimitContext);
                    settings = settings with
                    {
                        YLimit = (leftLimit, rightLimit)
                    };
                    break;
                }

                case "xlab":
                {
                    var stringContext = plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.StringContext>(0);
                    var visitor = new ComputableStringVisitor();
                    var cs = visitor.Visit(stringContext);
                    settings = settings with
                    {
                        XLabel = cs
                    };
                    break;
                }

                case "ylab":
                {
                    var stringContext = plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.StringContext>(0);
                    var visitor = new ComputableStringVisitor();
                    var cs = visitor.Visit(stringContext);
                    settings = settings with
                    {
                        YLabel = cs
                    };
                    break;
                }

                case "out":
                {
                    var outPath = argString.EndsWith(".png") ? argString : $"{argString}.png";
                    settings = settings with
                    {
                        OutPath = outPath
                    };
                    break;
                }

                case "grid":
                {
                    settings = settings with
                    {
                        ShowGrid = argString switch
                        {
                            "yes" => true,
                            "no" => false,
                            _ => true
                        }
                    };
                    break;
                }

                case "bg":
                {
                    settings = settings with
                    {
                        ShowBackground = argString switch
                        {
                            "yes" => true,
                            "no" => false,
                            _ => true
                        }
                    };
                    break;
                }

                case "gui":
                {
                    settings = settings with
                    {
                        ShowInGui = argString switch
                        {
                            "yes" => true,
                            "no" => false,
                            _ => true
                        }
                    };
                    break;
                }
                
                default:
                    // do nothing
                    break;
            }
        }

        return new PlotCommand
        {
            FunctionsToPlot = variableNames,
            Text = text,
            Settings = settings
        };
    }

    public override Statement VisitExpression(Unipi.MppgParser.Grammar.MppgParser.ExpressionContext context)
    {
        var expression = new Expression(context);
        var text = context.GetJoinedText();
        return new ExpressionCommand(expression) { Text = text };
    }

    public override Statement VisitAssignment(Unipi.MppgParser.Grammar.MppgParser.AssignmentContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var text = context.GetJoinedText();
        var name = context.GetChild(0).GetText();
        var expressionContext = (Unipi.MppgParser.Grammar.MppgParser.ExpressionContext) context.GetChild(2);
        var expression = new Expression(expressionContext);
          
        return new Assignment(name, expression) { Text = text };
    }

    public override Statement VisitPrintExpressionCommand(Unipi.MppgParser.Grammar.MppgParser.PrintExpressionCommandContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");
        
        var name = context.GetChild(2).GetText();
        var text = context.GetJoinedText();

        return new PrintExpressionCommand(name) { Text = text };
    }

    public override Statement VisitAssertion(Unipi.MppgParser.Grammar.MppgParser.AssertionContext context)
    {
        var leftExpressionContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.ExpressionContext>(0);
        var rightExpressionContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.ExpressionContext>(1);
        var operatorContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.AssertionOperatorContext>(0);
        var text = context.GetJoinedText();
        
        var leftExpression = new Expression(leftExpressionContext);
        var rightExpression = new Expression(rightExpressionContext);
        var operatorText = operatorContext.GetJoinedText(); 
        var @operator = operatorText switch
        {
            "=" => Assertion.AssertionOperator.Equal,
            "!=" => Assertion.AssertionOperator.NotEqual,
            "<" => Assertion.AssertionOperator.Less,
            "<=" => Assertion.AssertionOperator.LessOrEqual,
            ">" => Assertion.AssertionOperator.Greater,
            ">=" => Assertion.AssertionOperator.GreaterOrEqual,
            _ => throw new ArgumentException($"Operator '{operatorText}' not recognized")
        };

        return new Assertion(leftExpression, rightExpression, @operator){ Text = text };
    }
}