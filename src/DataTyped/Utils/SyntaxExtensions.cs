using DataTyped.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DataTyped.Generator;

public static class SyntaxExtensions
{
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

    public static string GetName(this INamespaceSymbol namespaceSymbol) =>
        namespaceSymbol.IsGlobalNamespace ? "" : namespaceSymbol.ToDisplayString();

    public static bool TryGetStringLiteral(this ExpressionSyntax expression, out string value)
    {
        if (expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            value = literal.Token.ValueText;
            return true;
        }

        value = null;
        return false;
    }
}
