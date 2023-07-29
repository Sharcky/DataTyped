using DataTyped.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace DataTyped.Generator;

public class AdditionalFilesGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(
            context.AnalyzerConfigOptionsProvider.Combine(
            context.AdditionalTextsProvider.Collect()), (spc, t) => Generate(spc, t.Left, t.Right));
    }

    private void Generate(SourceProductionContext context, AnalyzerConfigOptionsProvider compilation, ImmutableArray<AdditionalText> additionalTexts)
    {
        if (!additionalTexts.Any())
            return;

        // Horrible hack: https://stackoverflow.com/questions/65070796/source-generator-information-about-referencing-project#comment131143151_65093866
        // I don't understand why these options are right there yet the Roslyn API refuses to allow proper access to it
        compilation.GlobalOptions.TryGetValue("build_property.projectdir", out var projectPath);

        if (string.IsNullOrEmpty(projectPath))
            return;

        var files =
            from file in additionalTexts
            let fullPath     = file.Path
            let fileName     = Path.GetFileName(fullPath)
            let relativePath = file.Path.Replace(projectPath, "")
            let relativeDir  = relativePath.Replace(fileName, "")
            let fileNameWithUnderscores = fileName.Replace(",", "_")
            let identifier   = Identifier.PropertyName(fileName)
            orderby fullPath
            select new { FileName = fileName, RelativeDir = relativeDir, RelativePath = relativePath, Identifier = identifier };

        files = files.ToList();

        var rootClass = new Class { Name = "ProjectFiles", IsRoot = true };

        foreach (var file in files)
        {
            var targetClass = GetTargetClass(file.RelativeDir, rootClass);

            targetClass.Properties.Add((file.Identifier, file.RelativePath));
        }

        var code = Render(rootClass, string.Empty);
        context.AddSourceFile("ProjectFiles", code);
    }

    private Class GetTargetClass(string relativeDir, Class parentClass)
    {
        if (string.IsNullOrEmpty(relativeDir))
            return parentClass;

        var parts = relativeDir.Split(Path.DirectorySeparatorChar);

        if (!parts.Any())
            return parentClass;

        var className = Identifier.PropertyName(parts[0]);

        var target = 
            parentClass.InnerClasses.FirstOrDefault(x => x.Name == className);

        if (target is null)
        {
            target = new Class { Name = className };
            parentClass.InnerClasses.Add(target);
        }

        var rest = string.Join(Path.PathSeparator.ToString(), parts.Skip(1));

        return GetTargetClass(rest, target);

    }

    private string Render(Class cls, string indent)
    {
        var props = cls.Properties.Select(x => RenderProperty(x.PropertyName, x.Value, indent + "    ")).Join("");
        var innerclasses = cls.InnerClasses.Select(x => Render(x, indent + "    ")).Join("");
        var namespaceDeclaration =
            cls.IsRoot
            ? "namespace DataTyped;" + Environment.NewLine
            : "";
        
        return @$"{namespaceDeclaration}
{indent}public class {cls.Name}
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

    private class Class
    {
        public bool IsRoot { get; set; }

        public string Name { get; set; }

        public List<(string PropertyName, string Value)> Properties { get; } = new();

        public List<Class> InnerClasses { get; } = new();

        public override string ToString() => Name;
        
    }
}

