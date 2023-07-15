using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using DataTyped.Model;
using DataTyped.Parser;
using DataTyped.Renderer;

namespace DataTyped.Generator;

[Generator]
public partial class DataTypeGenerator : ISourceGenerator
{
    private const string JsonAttribute = "DataTyped.JsonTypeAttribute";
    private const string CsvAttribute = "DataTyped.CsvTypeAttribute";
    private const string XmlAttribute = "DataTyped.XmlTypeAttribute";
    private const string YamlAttribute = "DataTyped.YamlTypeAttribute";

    private const string JsonType = "DataTyped.Json";
    private const string CsvType = "DataTyped.Csv";
    private const string XmlType = "DataTyped.Xml";
    private const string YamlType = "DataTyped.Yaml";

    public static readonly string[] AttributeTypes = new[]
    {
        JsonAttribute, CsvAttribute, XmlAttribute
    };

    public static readonly string[] DataTypes = new[]
    {
        JsonType, CsvType, XmlType, YamlType
    };

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForPostInitialization((i) => i.AddSource("DataTyped.g.cs", SourceText.From(GeneratedAttributes.Text, Encoding.UTF8)));
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        try
        {
            if (context.SyntaxContextReceiver is not SyntaxReceiver receiver)
                return;

            var attributeSymbol = context.Compilation.GetTypeByMetadataName(JsonAttribute);
            if (attributeSymbol is null)
                return;

            var typeDefinitions =
                receiver.Result.TypeDefinitions.ToList();

            var typeDefinitionTasks =
                typeDefinitions
                    .ToList()
                    .Where(x => x.DataSource != null)
                    .Select(x => Task.Run(() => Execute(new ExecutionInput(context, x, typeDefinitions))))
                    .ToArray();

            var results =
                Task.WhenAll(typeDefinitionTasks)
                    .Result
                    .ToArray();

            var allTypes =
                results.OfType<ExecutionResult.Ok>()
                       .SelectMany(x => x.Types)
                       .GroupBy(x => x.FullName)
                       .Select(x => x.First())
                       .ToList();

            DefaultRenderer.Render(context, allTypes);

            var allErrors =
                results.OfType<ExecutionResult.Error>()
                       .ToList();

            foreach (var error in allErrors)
                context.ReportException(error.Exception);
        }
        catch (Exception ex)
        {
            context.ReportException(ex);
        }
    }

    private async Task<ExecutionResult> Execute(ExecutionInput input)
    {
        try
        {
            var (context, rootType, typeDefinitions) = input;

            if (rootType.DataSource!.Type is DataSourceType.Inferred inferred)
            {
                var additionalFile = context.AdditionalFiles.FirstOrDefault(x => Path.GetFileName(x.Path) == inferred.Location);
                if (additionalFile != null)
                    rootType.DataSource = rootType.DataSource with { Type = new DataSourceType.LocalFile(additionalFile.Path) };
                else if (File.Exists(inferred.Location))
                    rootType.DataSource = rootType.DataSource with { Type = new DataSourceType.LocalFile(inferred.Location) };
                else if (Uri.IsWellFormedUriString(inferred.Location, UriKind.Absolute))
                    rootType.DataSource = rootType.DataSource with { Type = new DataSourceType.Http(inferred.Location) };
                else
                    throw new InvalidOperationException($"Unable to determine data source type: '{inferred.Location}'");
            }

            TextReader? reader = null;
            if (rootType.DataSource.Type is DataSourceType.Http http)
            {
                var rawData = await Cache.Get(http.Uri, async () =>
                {
                    var client = new HttpClient();
                    var response = await client.GetAsync(http.Uri);
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                    return body;
                });
                reader = new StringReader(rawData);
            }
            else if (rootType.DataSource.Type is DataSourceType.LocalFile localFile)
            {
                reader = new StreamReader(File.OpenRead(localFile.FileName));
            }

            if (reader is null)
                throw new InvalidOperationException($"Unable to read data.");

            Parser.Parser parser =
                rootType.DataSource.Format switch
                {
                    Format.Json => new JsonParser(),
                    // TODO: Implement other format parsers.
                    _ => throw new InvalidOperationException($"Parser not found for format: {rootType.DataSource.Format.GetType().Name}")
                };

            var result = parser.Parse(reader, context, rootType, typeDefinitions);

            return new ExecutionResult.Ok(result);
        }
        catch (Exception ex)
        {
            return new ExecutionResult.Error(ex);
        }
    }

    private record ExecutionInput(GeneratorExecutionContext Context, TypeDefinition RootType, List<TypeDefinition> Types);

    private abstract record ExecutionResult()
    {
        public record Ok(List<TypeDefinition> Types): ExecutionResult();

        public record Error(Exception Exception): ExecutionResult();
    }

    private class SyntaxReceiver : ISyntaxContextReceiver
    {
        public CodeAnalysisResult Result { get; } = new();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node.IsGenerated())
                return;

            VisitClassDeclaration(context);
            VisitInvocation(context);
        }

        private void VisitClassDeclaration(GeneratorSyntaxContext context)
        {
            if (context.Node is not TypeDeclarationSyntax typeDeclaration || typeDeclaration.AttributeLists.Count == 0)
                return;

            var namedTypeSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration);
            if (namedTypeSymbol is null)
            {
                return;
            }

            var attributes = namedTypeSymbol.GetAttributes();
            var dataTypedAttributes = attributes.Where(ad => AttributeTypes.Contains(ad?.AttributeClass?.ToDisplayString())).ToArray();
            if (!dataTypedAttributes.Any())
                return;

            var attribute = dataTypedAttributes[0];
            var attributeTypeName = attribute.AttributeClass?.ToDisplayString();

            Format format =
                attributeTypeName switch
                {
                    JsonAttribute => new Format.Json(),
                    CsvAttribute => new Format.Csv(),
                    XmlAttribute => new Format.Xml(),
                    YamlAttribute => new Format.Yaml(),
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
                               

            Result.AddOrMerge(typeDefinition);
        }

        private void VisitInvocation(GeneratorSyntaxContext context)
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
            if (typeInfo.Symbol is null || !DataTypes.Contains(typeInfo.Symbol.ToDisplayString()))
                return;

            if (generic.TypeArgumentList.Arguments.Count != 1)
                return;

            if (generic.TypeArgumentList.Arguments[0] is not IdentifierNameSyntax typeName)
                return;

            if (invocation.ArgumentList.Arguments.Count < 1)
                return;

            if (invocation.ArgumentList.Arguments[0].Expression is not LiteralExpressionSyntax literal)
                return;

            if (!literal.IsKind(SyntaxKind.StringLiteralExpression))
                return;

            var urlOrFileName = literal.Token.ValueText;
            var namespaceName = invocation.GetContainingNamespace();

            Format format =
                typeInfo.Symbol.ToDisplayString() switch
                {
                    JsonType => new Format.Json(),
                    CsvType => new Format.Csv(),
                    XmlType => new Format.Xml(),
                    YamlType => new Format.Yaml(),
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

            Result.AddOrMerge(typeDefinition);
        }
    } 
}

