using DataTyped.Model;
using Microsoft.CodeAnalysis;
using System.Text;
using DataTyped.Generator;

namespace DataTyped.Renderer;

internal static class DefaultRenderer
{
    public static void Render(SourceProductionContext context, List<TypeDefinition> types)
    {
        var groupedByNamespace = 
            types.GroupBy(x => x.Namespace.IsNullOrEmpty() ? "" : x.Namespace)
                 .ToList();

        var index = 0;

        foreach (var group in groupedByNamespace)
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                !group.Key.IsNullOrEmpty()
                ? $"namespace {group.Key};"
                : "");

            sb.AppendLine();

            sb.AppendLine(
                group.Select(Render)
                     .Join(Environment.NewLine));

            var fileName =
                !group.Key.IsNullOrEmpty()
                ? $"{group.Key}.Types"
                : $"DataTyped.Types{(index > 0 ? index.ToString() : "")}";
            context.AddSourceFile(fileName, sb.ToString());
            index++;
        }
    }

    private static string Render(TypeDefinition type)
    {
        var properties =
            type.Properties
                .Where(x => !x.IsExplicit)
                .Select(Render)
                .Join(Environment.NewLine);

        var typeKeyword = type.IsRecord ? "record" : "class";

        return $@"
{Render(type.AccessModifier)} partial {typeKeyword} {type.Name}
{{
{properties}
}}";
    }

    private static string Render(PropertyDefinition property)
    {
        var attributes =
            property.Attributes
                    .Select(Render)
                    .Join(Environment.NewLine);

        var sb = new StringBuilder();
        sb.Append(attributes);
        sb.AppendLine();
        sb.AppendLine($"    public {property.Type} {property.Name} {{ get; set; }}");
        return sb.ToString();
    }

    private static string Render(AttributeDefinition attribute)
    {
        var parameters = 
            attribute.Parameters
                     .Select(Render)
                     .Join(", ");

        return $"    [{attribute.Name}({parameters})]";
    }

    private static string Render(ParameterDefinition parameter)
    {
        var result = "";
        if (parameter is NamedParameterDefinition namedParameter)
            result += $"{namedParameter.Name} = ";

        if (parameter.Value is int or decimal or double or long)
            result += $"{parameter.Value}";
        else
            result += $"\"{parameter.Value}\"";
        return result;
    }

    private static string Render(AccessModifier? accessModifier) =>
        accessModifier switch
        {
            null => "",
            AccessModifier.Private => "private",
            AccessModifier.Protected => "protected",
            AccessModifier.Public => "public",
            AccessModifier.Internal => "internal",
            AccessModifier.PrivateProtected => "private protected",
            AccessModifier.ProtectedInternal => "protected internal",
            _ => throw new InvalidOperationException($"Unsupported access modifier: {accessModifier}")
        };


}
