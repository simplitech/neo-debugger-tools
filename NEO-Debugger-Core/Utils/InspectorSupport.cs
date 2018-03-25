using Neo.Debugger.Core.Data;
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Neo.Debugger.Core.Utils
{
    public class VariableAssignement
    {
        public string varName;
        public int lineNumber;

        public override string ToString()
        {
            return lineNumber + " => " + varName;
        }
    }

    public class InspectorWalker : CSharpSyntaxWalker
    {
        private List<VariableAssignement> assignements;

        public InspectorWalker(List<VariableAssignement> assignements) : base(SyntaxWalkerDepth.Node)
        {
            this.assignements = assignements;
        }

        private void AddLine(SyntaxNode node, string name)
        {
            var position = node.SpanStart;
            var text = node.GetText();

            var loc = node.GetLocation();
            var lineSpan = loc.SourceTree.GetLineSpan(loc.SourceSpan);
            var line = lineSpan.StartLinePosition.Line + 1;

            assignements.Add(new VariableAssignement() { lineNumber = line, varName = name });
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            base.VisitMethodDeclaration(node);

            foreach (var st in node.Body.Statements)
            {
                var t = st.GetType();
                System.Diagnostics.Debug.WriteLine(t.Name);
            }
        }

        public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            base.VisitLocalDeclarationStatement(node);

            foreach (VariableDeclaratorSyntax entry in node.Declaration.Variables)
            {
                if (entry.Initializer != null)
                {
                    AddLine(entry, entry.Identifier.ToString());
                }
            }
        }

        public override void VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            base.VisitExpressionStatement(node);

            var s = (AssignmentExpressionSyntax)node.Expression;

            AddLine(node, s.Left.ToString());
        }

    }

    public static class InspectorSupport
    {
        public static List<VariableAssignement> ParseAssigments(string sourceCode, SourceLanguage language)
        {
            var result = new List<VariableAssignement>();
            switch (language)
            {
                case SourceLanguage.CSharp:
                    {
                        var options = new CSharpParseOptions(kind: SourceCodeKind.Regular, languageVersion: LanguageVersion.Latest);
                        SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceCode, options);

                        var walker = new InspectorWalker(result);
                        var root = tree.GetRoot();
                        walker.Visit(root);
                        break;
                    }
            }
            return result;
        }
    }
}
