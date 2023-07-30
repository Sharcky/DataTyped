using DataTyped.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Directory = DataTyped.Model.AdditionalFiles.Directory;

namespace DataTyped.Generator;

public class AdditionalFilesGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(
            context.AnalyzerConfigOptionsProvider.Combine(
            context.AdditionalTextsProvider.Collect()), (spc, t) => Generate(spc, t.Left, t.Right));
    }

    private void Generate(SourceProductionContext context, AnalyzerConfigOptionsProvider analyzerConfig, ImmutableArray<AdditionalText> additionalTexts)
    {
        if (!additionalTexts.Any())
            return;

        var projectPath = analyzerConfig.GetProjectRootDirectory();
        if (string.IsNullOrEmpty(projectPath))
            return;

        var rootDirectory = AdditionalFiles.Create(additionalTexts, projectPath!);

        var code = Render(rootDirectory, string.Empty);
        context.AddSourceFile("ProjectFiles", code);
    }

    private string Render(Directory directory, string indent)
    {
        var props = directory.Files.Select(x => RenderProperty(x.Identifier, x.RelativePath, indent + "    ")).Join("");
        var innerclasses = directory.SubDirectories.Select(x => Render(x, indent + "    ")).Join("");
        var namespaceDeclaration =
            directory.IsRoot
            ? "namespace DataTyped;" + Environment.NewLine
            : "";
        
        return @$"{namespaceDeclaration}
{indent}public class {directory.Name}
{indent}{{
{props}
{innerclasses}
{indent}}}
";
    }

    private string RenderProperty(string propertyName, string value, string indent)
    {
        return @$"
{indent}public const string {propertyName} = @""{value}"";
";
    }
}

