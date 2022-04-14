using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AnalyzerSecond
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AnalyzerSecondCodeFixProvider)), Shared]
    public class AnalyzerSecondCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AnalyzerSecondAnalyzer.DiagnosticId); }
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
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();
            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
    CodeAction.Create(
        title: CodeFixResources.CodeFixTitle,
        createChangedDocument: c => MakeConstAsync(context.Document, declaration, c),
        equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
    diagnostic);
        }

        private static async Task<Document> MakeConstAsync(Document document,
    MethodDeclarationSyntax localDeclaration,
    CancellationToken cancellationToken)
        {
            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            //  Create a class: (class Order)
            var classDeclaration = SyntaxFactory.ClassDeclaration("ClassFor"+ localDeclaration.Identifier.ValueText);

            // Add the public modifier: (public class Order)
            classDeclaration = classDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            var parametrs = localDeclaration.ParameterList.Parameters.Where(g => g.Modifiers.Any(SyntaxKind.OutKeyword));
            foreach(var parametr in parametrs)
            {
                var propertyDeclaration = SyntaxFactory.PropertyDeclaration(parametr.Type, parametr.Identifier.Text.ToCharArray().First().ToString().ToUpper() + parametr.Identifier.Text.Substring(1))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
                propertyDeclaration = propertyDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
                classDeclaration = classDeclaration.AddMembers(propertyDeclaration);
            }
            var constructorDeclaration = SyntaxFactory.ConstructorDeclaration("ClassFor" + localDeclaration.Identifier.ValueText);
            foreach(var parametr in parametrs)
            {
                constructorDeclaration = constructorDeclaration.AddParameterListParameters(SyntaxFactory.Parameter(parametr.Identifier).WithType(parametr.Type));
            }
            constructorDeclaration = constructorDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            foreach (var parametr in parametrs)
            {
                constructorDeclaration = constructorDeclaration.AddBodyStatements(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName(parametr.Identifier.Text.ToCharArray().First().ToString().ToUpper() + parametr.Identifier.Text.Substring(1)), SyntaxFactory.IdentifierName(parametr.Identifier))));
            }
            classDeclaration = classDeclaration.AddMembers(constructorDeclaration);
            // Return document with transformed tree.
            List<SyntaxNode> nodes = new List<SyntaxNode>();
            nodes.Add(classDeclaration);
            var newMethod = localDeclaration.WithIdentifier(localDeclaration.Identifier);
            var firstBodyElements = newMethod.Body.Statements.First();
            List<SyntaxNode> localDeclarParam = new List<SyntaxNode>(); 
            foreach(var parametr in parametrs)
            {
                var var = SyntaxFactory.VariableDeclaration(parametr.Type).AddVariables(SyntaxFactory.VariableDeclarator(parametr.Identifier.ValueText));
                var locvar = SyntaxFactory.LocalDeclarationStatement(var);
                localDeclarParam.Add(locvar);
            }
            newMethod = newMethod.InsertNodesBefore(firstBodyElements, localDeclarParam);
            var oldParamList = newMethod.ParameterList.Parameters;
            var newParamList = new List<ParameterSyntax>();
            foreach (var parametr in newMethod.ParameterList.Parameters.Where(g => !g.Modifiers.Any(SyntaxKind.OutKeyword)))
            {
                newParamList.Add(parametr);
            }
            var newParam = SyntaxFactory.Parameter(SyntaxFactory.Identifier("outparam")).WithType(SyntaxFactory.IdentifierName(classDeclaration.Identifier.ValueText));
            newParamList.Add(newParam);
            newMethod = newMethod.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList<ParameterSyntax>(newParamList)));
            string s = "outparam = new ClassFor" + localDeclaration.Identifier.ValueText + "(";
            foreach (var parametr in parametrs)
            {
                s = s + parametr.Identifier.ValueText + ", ";
            }
            s = s.Remove(s.Length - 2, 2);
            s = s + ");\n";
            var syntax = SyntaxFactory.ParseStatement(s);
            List<SyntaxNode> lastnode = new List<SyntaxNode>();
            lastnode.Add(syntax);
            if (newMethod.Body.Statements.Last().Kind() == SyntaxKind.ReturnStatement)
            {
                newMethod = newMethod.InsertNodesBefore(newMethod.Body.Statements.Last(), lastnode);
            }
            else
            {
                newMethod = newMethod.InsertNodesAfter(newMethod.Body.Statements.Last(), lastnode);
            }
            SyntaxNode newRoot = oldRoot.ReplaceNode(localDeclaration, newMethod);
            newRoot = newRoot.InsertNodesBefore(newRoot.DescendantNodes()
                                        .OfType<MethodDeclarationSyntax>().SingleOrDefault(g => g.Identifier.ValueText.Equals(newMethod.Identifier.ValueText)), nodes);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
