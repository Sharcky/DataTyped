using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using DataTyped.Model;
using System.Collections.Immutable;

namespace DataTyped.Generator;

public class InvocationAggregator : Aggregator<InvocationExpressionSyntax, CodeAnalysisResult>
{
    public ImmutableArray<AdditionalText> AdditionalFiles { get; set; }

    public override bool Filter(SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is not InvocationExpressionSyntax invocation)
            return false;

        if (invocation.Expression is not MemberAccessExpressionSyntax maes)
            return false;

        if (maes.Expression is not IdentifierNameSyntax owner)
            return false;

        if (maes.Name is not GenericNameSyntax generic)
            return false;

        return true;
    }

    public override void Visit(GeneratorSyntaxContext context, CodeAnalysisResult result)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
            return;

        if (invocation.Expression is not MemberAccessExpressionSyntax maes)
            return;

        if (maes.Expression is not IdentifierNameSyntax owner)
            return;

        if (maes.Name is not GenericNameSyntax generic)
            return;

        var typeInfo = context.SemanticModel.GetSymbolInfo(owner);
        if (typeInfo.Symbol is null || !Attributes.DataTypes.Contains(typeInfo.Symbol.ToDisplayString()))
            return;

        if (generic.TypeArgumentList.Arguments.Count != 1)
            return;

        if (generic.TypeArgumentList.Arguments[0] is not IdentifierNameSyntax typeName)
            return;

        if (invocation.ArgumentList.Arguments.Count < 1)
            return;

        var urlExpression = invocation.ArgumentList.Arguments[0].Expression;

        urlExpression.TryGetStringLiteral(out var urlOrFileName);
        if (urlOrFileName == null && urlExpression is MemberAccessExpressionSyntax urlMaes)
        {
            var fullMaes = urlMaes.ToFullString();
            if (fullMaes.StartsWith("ProjectFiles"))
                urlOrFileName = fullMaes;
        }

        if (string.IsNullOrEmpty(urlOrFileName))
            return;

        var namespaceName = invocation.GetContainingNamespace();

        Format format =
            typeInfo.Symbol.ToDisplayString() switch
            {
                Attributes.JsonType => new Format.Json(),
                Attributes.CsvType => new Format.Csv(),
                Attributes.XmlType => new Format.Xml(),
                Attributes.YamlType => new Format.Yaml(),
                _ => throw new InvalidOperationException($"Unsupported Data Type: {typeInfo.Symbol.ToDisplayString()}")
            };

        var dataSourceType = new DataSourceType.Inferred(urlOrFileName);

        var typeDefinition = new TypeDefinition(typeName.Identifier.Text)
        {
            Namespace = namespaceName,
            AccessModifier = AccessModifier.Public,
            IsRecord = false,
            DataSource = new DataSource(dataSourceType, format),
            IsExplicit = true,
        };

        result.AddOrMerge(typeDefinition);
    }
}

