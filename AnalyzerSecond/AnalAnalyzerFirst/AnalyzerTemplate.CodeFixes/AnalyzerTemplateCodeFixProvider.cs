using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace AnalyzerTemplate
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AnalyzerTemplateCodeFixProvider)), Shared]
    public class AnalyzerTemplateCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AnalyzerTemplateAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
     CodeAction.Create(
         title: CodeFixResources.CodeFixTitle,
         createChangedDocument: c => MakeConstAsync(context.Document, declaration, c),
         equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
     diagnostic);
        }

        private static async Task<Document> MakeConstAsync(Document document,
    LocalDeclarationStatementSyntax localDeclaration,
    CancellationToken cancellationToken)
        {
            var node = localDeclaration.Declaration.Variables.SingleOrDefault(g => g.Identifier.ValueText.Length > 3 && g.Identifier.ValueText.StartsWith("not"));
            var newtoken = SyntaxFactory.Identifier(node.Identifier.LeadingTrivia, node.Identifier.Kind(), node.Identifier.Text.Substring(3).ToLower(), node.Identifier.ValueText.Substring(3).ToLower(), node.Identifier.TrailingTrivia);
            var newnode = node.WithIdentifier(newtoken);
            if (!(node.Initializer is null))
            {
                var newtoken2 = SyntaxFactory.EqualsValueClause(node.Initializer.EqualsToken, SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, node.Initializer.Value));
                newnode = newnode.WithInitializer(newtoken2);
            }
            var newvardec = SyntaxFactory.VariableDeclaration(localDeclaration.Declaration.Type, localDeclaration.Declaration.Variables.Replace(node, newnode));
            var newlocdec = localDeclaration.WithDeclaration(newvardec);
            // Replace the old local declaration with the new local declaration.
            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            SyntaxNode newRoot = oldRoot.ReplaceNode(localDeclaration, newlocdec);
            var allNodesWithPar = from methodDeclaration in newRoot.DescendantNodes()
                                        .OfType<IdentifierNameSyntax>()
                                 where methodDeclaration.Identifier.ValueText.Equals(node.Identifier.ValueText)
                                 select methodDeclaration;
            while(allNodesWithPar.Count() != 0)
            {
                var fr = allNodesWithPar.First();
                var newfr = fr.WithIdentifier(SyntaxFactory.Identifier(fr.Identifier.LeadingTrivia, fr.Identifier.Kind(), fr.Identifier.Text.Substring(3).ToLower(), fr.Identifier.ValueText.Substring(3).ToLower(), fr.Identifier.TrailingTrivia));
                var par = fr.Parent;
                SyntaxNode newpapa;
                if (par.Kind() == SyntaxKind.SimpleAssignmentExpression)
                {
                    newpapa = par.ReplaceNode(fr, newfr);
                    var z = newpapa.ChildNodes();
                    var der = z.Last();
                    newpapa = newpapa.ReplaceNode(z.Last(), SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, (ExpressionSyntax)der));
                }
                else
                {
                    newpapa = par.ReplaceNode(fr, SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, newfr));
                }
                newRoot = newRoot.ReplaceNode(par, newpapa);
                allNodesWithPar = from methodDeclaration in newRoot.DescendantNodes()
                                        .OfType<IdentifierNameSyntax>()
                                  where methodDeclaration.Identifier.ValueText.Equals(node.Identifier.ValueText)
                                  select methodDeclaration;
            }
            // Return document with transformed tree.
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
