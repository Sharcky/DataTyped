using DataTyped.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Composition;

namespace DataTyped.CodeAnalysis;

public static class Rules
{
    public const string Category = "DataTyped";

    public const string DataUntypedId = "DT001";

    public static readonly DiagnosticDescriptor DataUntyped =
        new DiagnosticDescriptor(
            DataUntypedId,
            "Data is untyped",
            "Data is untyped long description",
            Category,
            DiagnosticSeverity.Warning,
            true,
            description: "Use the Code Fix to enable strong types for this data.");
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DataUntypedAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rules.DataUntyped);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(Execute, SyntaxKind.InvocationExpression);
    }

    private void Execute(SyntaxNodeAnalysisContext context)
    {
        if (context.IsGenerated())
            return;

        if (context.Node is not InvocationExpressionSyntax invocation)
            return;

        if (invocation.Expression is not MemberAccessExpressionSyntax maes)
            return;

        if (maes.Expression is not IdentifierNameSyntax owner)
            return;

        if (maes.Name is GenericNameSyntax)
            return;

        var typeInfo = context.SemanticModel.GetSymbolInfo(owner);

        if (typeInfo.Symbol?.ContainingNamespace?.Name is not "DataTyped")
            return;

        if (!DataTypeGenerator.DataTypes.Contains(typeInfo.Symbol?.ToDisplayString()))
            return;

        if (invocation.ArgumentList.Arguments.Count != 1)
            return;

        if (invocation.ArgumentList.Arguments[0].Expression is not LiteralExpressionSyntax literal)
            return;

        if (!literal.IsKind(SyntaxKind.StringLiteralExpression))
            return;

        var declarator = invocation.FirstAncestorOrSelf<VariableDeclaratorSyntax>();

        var assignment = invocation.FirstAncestorOrSelf<AssignmentExpressionSyntax>();

        if (declarator is null && assignment is null)
            return;

        var diagnostic = Diagnostic.Create(Rules.DataUntyped, invocation.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }
}

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DataUntypedCodeFixProvider)), Shared]
public class DataUntypedCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Rules.DataUntypedId);

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the type declaration identified by the diagnostic.
        var invocation = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();

        if (invocation is null)
            return;

        // Register a code action that will invoke the fix.
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Use Typed Data", 
                (c) => AddTypeAsync(context.Document, invocation, c))
            , diagnostic);
                
    }

    private async Task<Document> AddTypeAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
    {
        var declarator = invocation.FirstAncestorOrSelf<VariableDeclaratorSyntax>();

        var assignment = invocation.FirstAncestorOrSelf<AssignmentExpressionSyntax>();

        if (declarator is null && assignment is null)
            return document;

        var variableName =
            (declarator?.Identifier, assignment?.Left) switch
            {
                (null, IdentifierNameSyntax l) => l.Identifier.Text,
                (null, MemberAccessExpressionSyntax m) => m.Name.Identifier.Text,
                (SyntaxToken d, null) => d.Text,
                _ => throw new InvalidOperationException($"Could not determine identifier: {invocation}")
            };

        variableName = variableName.Substring(0, 1).ToUpper() + variableName.Substring(1);

        if (invocation.Expression is not MemberAccessExpressionSyntax maes)
            return document;

        if (maes.Name is not IdentifierNameSyntax methodName)
            return document;

        maes = maes.WithName(
            SyntaxFactory.GenericName(
                SyntaxFactory.Identifier(methodName.Identifier.Text), 
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList<TypeSyntax>(new[]
                    {
                        SyntaxFactory.IdentifierName(variableName)
                    })
                )
            )
        );

        var newNode = invocation.WithExpression(maes);
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        var newRoot = root.ReplaceNode(invocation, newNode);

        return document.WithSyntaxRoot(newRoot);

    }
}