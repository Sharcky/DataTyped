using System.Text;
using DataTyped.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace DataTyped.Generator;

public static class AnalyzerExtensions
{
    public static void AddSourceFile(this GeneratorExecutionContext context, string identifier, string fileContent)
    {
        if (string.IsNullOrEmpty(fileContent))
            return;

        context.AddSource($"{identifier}.g.cs", SourceText.From(fileContent, Encoding.UTF8));
    }

    public static bool EqualsInvariant(this string? str, string? other) =>
        str is null 
        ? other is null
        : str.Equals(other, StringComparison.InvariantCulture);

    public static bool IsGenerated(this SyntaxNodeAnalysisContext context) =>
        context.IsGeneratedCode || context.Node.IsGenerated();

    public static bool IsGenerated(this SyntaxNode node) =>
        node.SyntaxTree?.FilePath is not { } f || f.Contains(".g.cs");

    public static string? GetContainingNamespace(this SyntaxNode node)
    {
        if (node == null) 
            return null;

        var namespaceName =
            node.SyntaxTree
                .GetRoot()
                .ChildNodes()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault()
                ?.Name.ToFullString();

        return namespaceName;
    }

    public static T? GetAttributeValue<T>(this AttributeData attribute, string attributeName)
    {
        var argument = attribute.NamedArguments.FirstOrDefault(x => x.Key == attributeName);
        if (argument.Value.Value is T value)
            return value;
        return default;
    }

    public static AccessModifier? GetAccessModifier(this TypeDeclarationSyntax typeDeclaration)
    {
        AccessModifier? accessModifier = null;
        if (typeDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword))
            accessModifier = AccessModifier.Public;
        else if (typeDeclaration.Modifiers.Any(SyntaxKind.InternalKeyword))
            accessModifier = AccessModifier.Internal;

        return accessModifier;
    }

    public static void ReportException(this GeneratorExecutionContext context, Exception ex)
    {
        // Report a diagnostic if an exception occurs while generating code; allows consumers to know what is going on
        string message = $"Exception: {ex}";
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
                "DT000",
                message,
                message,
                "DataTyped",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true),
            Location.None));
    }

    public static string GetName(this INamespaceSymbol namespaceSymbol) =>
        namespaceSymbol.IsGlobalNamespace ? "" : namespaceSymbol.ToDisplayString();
}

public static class StringExtensions
{
    public static string Join(this IEnumerable<string> source, string? separator = null) =>
        string.Join(separator ?? Environment.NewLine, source);

    public static bool IsNullOrEmpty(this string? str) => string.IsNullOrEmpty(str);

    public static bool IsIn(this string str, IEnumerable<string> arr) => arr.Contains(str);
}