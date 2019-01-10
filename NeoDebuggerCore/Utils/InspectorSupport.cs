using Neo.Debugger.Core.Data;
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Neo.Emulation.Emulator;
using Neo.Emulation;

namespace Neo.Debugger.Core.Utils
{
    public class InspectorWalker : CSharpSyntaxWalker
    {
        private Dictionary<int, Assignment> assignements;

        public InspectorWalker(Dictionary<int, Assignment> assignements) : base(SyntaxWalkerDepth.Node)
        {
            this.assignements = assignements;
        }

        private Emulator.Type ConvertType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return Emulator.Type.Unknown;
            }
            else
            switch (typeName.ToLower())
            {
                    case "byte[]": return Emulator.Type.ByteArray;
                    case "string": return Emulator.Type.String;
                    case "bool": return Emulator.Type.Boolean;
                    case "int":
                    case "uint":
                    case "long":
                    case "ulong":
                    case "biginteger":
                        return Emulator.Type.ByteArray;
                    default:
                        if (typeName.EndsWith("[]"))
                        {
                            return Emulator.Type.Array;
                        }
                        return Emulator.Type.Unknown;
            }

        }

        private void AddLine(SyntaxNode node, string name, Emulator.Type type)
        {
            var position = node.SpanStart;
            var text = node.GetText();

            var loc = node.GetLocation();
            var lineSpan = loc.SourceTree.GetLineSpan(loc.SourceSpan);
            var line = lineSpan.StartLinePosition.Line + 1;

            assignements[line] = new Assignment() { name = name, type = type };
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            base.VisitMethodDeclaration(node);

            if (node.Body == null)
            {
                return;
            }

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
                    var typeNode = entry.Parent as VariableDeclarationSyntax;
                    
                    AddLine(entry, entry.Identifier.ToString(), ConvertType(typeNode!=null ? typeNode.Type.ToString(): null));
                }
            }
        }

        public override void VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            base.VisitExpressionStatement(node);

            var s = node.Expression as AssignmentExpressionSyntax;

            if (s != null)
            {
                AddLine(node, s.Left.ToString(), Emulator.Type.Unknown);
            }
        }

    }

    public static class InspectorSupport
    {
        public static Dictionary<int, Assignment> ParseAssigments(string sourceCode, SourceLanguage language)
        {
            var result = new Dictionary<int, Assignment>();
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
