using Microsoft.CodeAnalysis;
using DataTyped.Model;
using DataTyped.Renderer;
using DataTyped.Parser;
using DataTyped.Utils;
using System.Collections.Immutable;

namespace DataTyped.Generator;

public class TypesGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var aggregator1 = new ClassDeclarationAggregator();
        var aggregator2 = new InvocationAggregator();

        var classes = context.Apply(aggregator1);
        var methods = context.Apply(aggregator2);

        var merged = classes.Merge(methods);

        var final =
            merged.Combine(context.AdditionalTextsProvider.Collect())
                  .Combine(context.CompilationProvider)
                  .Combine(context.AnalyzerConfigOptionsProvider)
                  .Flatten();

        context.RegisterSourceOutput(final, (spc, tuple) =>
        {
            var (data, additionalFiles, compilation, options) = tuple;
            var result = new CodeAnalysisResult();

            aggregator2.AdditionalFiles = additionalFiles;

            foreach (var d in data)
            {
                aggregator1.Aggregate(data, result);
                aggregator2.Aggregate(data, result);
            }

            var projectRootPath = options.GetProjectRootDirectory();

            Render(spc, compilation, projectRootPath, additionalFiles, result);
        });
    }
    
    private void Render(SourceProductionContext context, Compilation compilation, string projectRootPath, ImmutableArray<AdditionalText> additionalFiles, CodeAnalysisResult result)
    {
        try
        {
            var attributeSymbol = compilation.GetTypeByMetadataName(Attributes.JsonAttribute);
            if (attributeSymbol is null)
                return;

            var typeDefinitions = result.TypeDefinitions.ToList();

            var typeDefinitionTasks =
                typeDefinitions
                    .ToList()
                    .Where(x => x.DataSource != null)
                    .Select(x => Task.Run(() => Execute(new ExecutionInput(context, projectRootPath, additionalFiles, x, typeDefinitions))))
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
            if (input.RootType.DataSource!.Type is DataSourceType.Inferred inferred)
            {
                var fileList = AdditionalFiles.CreateList(input.AdditionalFiles, input.ProjectPath);
                var matchByRelativePath = fileList.FirstOrDefault(x => x.RelativePath == inferred.Location);
                
                if (inferred.Location.StartsWith("ProjectFiles"))
                {
                    var file = fileList.FirstOrDefault(x => x.FullyQualifiedIdentifier == inferred.Location);
                    if (file != null)
                        input.RootType.DataSource = input.RootType.DataSource with { Type = new DataSourceType.LocalFile(file.FullPath) };
                }
                else if (matchByRelativePath != null)
                    input.RootType.DataSource = input.RootType.DataSource with { Type = new DataSourceType.LocalFile(matchByRelativePath.FullPath) };
                else if (File.Exists(inferred.Location))
                    input.RootType.DataSource = input.RootType.DataSource with { Type = new DataSourceType.LocalFile(inferred.Location) };
                else if (Uri.IsWellFormedUriString(inferred.Location, UriKind.Absolute))
                    input.RootType.DataSource = input.RootType.DataSource with { Type = new DataSourceType.Http(inferred.Location) };
                else
                    throw new InvalidOperationException($"Unable to determine data source type: '{inferred.Location}'");
            }

            TextReader? reader = null;
            if (input.RootType.DataSource.Type is DataSourceType.Http http)
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
            else if (input.RootType.DataSource.Type is DataSourceType.LocalFile localFile)
            {
                reader = new StreamReader(File.OpenRead(localFile.FileName));
            }

            if (reader is null)
                throw new InvalidOperationException($"Unable to read data.");

            Parser.Parser parser =
                input.RootType.DataSource.Format switch
                {
                    Format.Json => new JsonParser(),
                    // TODO: Implement other format parsers.
                    _ => throw new InvalidOperationException($"Parser not found for format: {input.RootType.DataSource.Format.GetType().Name}")
                };

            var result = parser.Parse(reader, input.Context, input.RootType, input.Types);

            return new ExecutionResult.Ok(result);
        }
        catch (Exception ex)
        {
            return new ExecutionResult.Error(ex);
        }
    }

    private record ExecutionInput(SourceProductionContext Context, string ProjectPath, ImmutableArray<AdditionalText> AdditionalFiles, TypeDefinition RootType, List<TypeDefinition> Types);

    private abstract record ExecutionResult()
    {
        public record Ok(List<TypeDefinition> Types) : ExecutionResult();

        public record Error(Exception Exception) : ExecutionResult();
    }
}

