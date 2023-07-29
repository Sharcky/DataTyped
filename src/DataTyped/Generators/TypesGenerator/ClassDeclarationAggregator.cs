using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using DataTyped.Model;
using System.Collections.Immutable;

namespace DataTyped.Generator;

public class ClassDeclarationAggregator : Aggregator<ClassDeclarationSyntax, CodeAnalysisResult>
{
    public override bool Filter(SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is not ClassDeclarationSyntax cls)
            return false;

        return cls.AttributeLists.Count > 0;
    }

    public override void Visit(GeneratorSyntaxContext context, CodeAnalysisResult output)
    {
        if (context.Node is not TypeDeclarationSyntax typeDeclaration || typeDeclaration.AttributeLists.Count == 0)
            return;

        var namedTypeSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration);
        if (namedTypeSymbol is null)
        {
            return;
        }

        var attributes = namedTypeSymbol.GetAttributes();
        var dataTypedAttributes = attributes.Where(ad => Attributes.AttributeTypes.Contains(ad?.AttributeClass?.ToDisplayString())).ToArray();
        if (!dataTypedAttributes.Any())
            return;

        var attribute = dataTypedAttributes[0];
        var attributeTypeName = attribute.AttributeClass?.ToDisplayString();

        Format format =
            attributeTypeName switch
            {
                Attributes.JsonAttribute => new Format.Json(),
                Attributes.CsvAttribute => new Format.Csv(),
                Attributes.XmlAttribute => new Format.Xml(),
                Attributes.YamlAttribute => new Format.Yaml(),
                _ => throw new InvalidOperationException($"Unsupported Attribute Type: {attributeTypeName}")
            };

        var className = typeDeclaration.Identifier.Text;
        var namespaceName = namedTypeSymbol.ContainingNamespace.GetName();
        var accessModifier = typeDeclaration.GetAccessModifier();
        var isRecord = typeDeclaration is RecordDeclarationSyntax;

        var typeDefinition = new TypeDefinition(className)
        {
            Namespace = namespaceName,
            AccessModifier = accessModifier,
            IsRecord = isRecord,
        };

        var fileName = attribute.GetAttributeValue<string>(nameof(JsonTypeAttribute.Filename));
        if (fileName != null)
        {
            typeDefinition.DataSource = new DataSource(new DataSourceType.LocalFile(fileName), format);
        }

        var url = attribute.GetAttributeValue<string>(nameof(JsonTypeAttribute.Url));
        if (url != null)
        {
            // Has URL Argument, this means this is a root type defined in a remote URL
            typeDefinition.DataSource = new DataSource(new DataSourceType.Http(url), format);
        }

        var name = attribute.GetAttributeValue<string>(nameof(JsonTypeAttribute.Name));
        if (name != null)
        {
            // Has Name argument, this means this is a nested type
            typeDefinition.OriginalName = name;
        }

        var properties =
            typeDeclaration.Members
                           .OfType<PropertyDeclarationSyntax>()
                           .ToList();

        foreach (var property in properties)
        {
            var propertyType = context.SemanticModel.GetSymbolInfo(property.Type);
            if (propertyType.Symbol is null)
                continue;

            var propertyTypeName = propertyType.Symbol.ToDisplayString();

            var propertyDefinition =
                new PropertyDefinition(property.Identifier.Text, propertyTypeName, null, null)
                {
                    IsExplicit = true,
                };

            typeDefinition.Properties.Add(propertyDefinition);
        }

        output.AddOrMerge(typeDefinition);
    }
}

