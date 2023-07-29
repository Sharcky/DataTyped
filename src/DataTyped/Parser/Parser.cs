using Microsoft.CodeAnalysis;
using Pluralize.NET;
using System.Text.RegularExpressions;
using DataTyped.Model;

namespace DataTyped.Parser;

public abstract class Parser
{
    protected static readonly IPluralize pluralizer = new Pluralizer();
    protected static readonly Regex parseNumberFromPropertyName = new Regex("(.*Property)([0-9]+)", RegexOptions.Compiled);
    protected static readonly char[] forbiddenCharacters = new[] { ' ', '-', ':', ';', '.' };

    /// <summary>
    /// Gets a name that is valid in C# and makes it Pascal-case.
    /// Optionally, it can singularize the name, so that a list property has a proper model class.
    /// E.g. Cars will have a model type of Car.
    /// </summary>
    /// <param name="name">The type name that is possibly not valid in C#</param>
    /// <param name="singularize">If true, the name will be singularized if it is plural</param>
    /// <returns>A valid C# Pascal-case name</returns>
    protected string GetValidName(string name, bool singularize = false)
    {
        // Make a plural form singular using Pluralize.NET
        if (singularize && pluralizer.IsPlural(name))
        {
            name = pluralizer.Singularize(name);
        }

        return Identifier.PropertyName(name);
    }

    protected string RenameIfDuplicateOrConflicting(string propertyName, TypeDefinition classModel)
    {
        const string postFix = "Property";
        string newPropertyName = propertyName;
        if (string.Equals(propertyName, classModel.Name, StringComparison.InvariantCulture))
        {
            // Property name conflicts with class name, so add a postfix
            newPropertyName = $"{propertyName}{postFix}";
        }

        while (classModel.Properties.Any(p => p.Name == newPropertyName))
        {
            var match = parseNumberFromPropertyName.Match(newPropertyName);
            if (match.Success)
            {
                newPropertyName = $"{match.Groups[1].Value}{int.Parse(match.Groups[2].Value) + 1}";
            }
            else
            {
                newPropertyName = $"{newPropertyName}2";
            }
        }

        return newPropertyName;
    }

    public abstract List<TypeDefinition> Parse(TextReader reader, SourceProductionContext context, TypeDefinition rootType, List<TypeDefinition> types);
}
