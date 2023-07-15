using Microsoft.CodeAnalysis;
using Pluralize.NET;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using DataTyped.Generator;
using DataTyped.Model;
using System.Text;

namespace DataTyped.Parser;

public abstract class Parser
{
    /// <summary>
    /// Contains C# reserved keywords defined in MSDN documentation at <a href="http://msdn.microsoft.com/en-us/library/x53a06bb.aspx"/>.
    /// </summary>
    private static readonly HashSet<string> ReservedKeywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
            "checked", "class", "const", "continue", "decimal", "default", "delegate", "do",
            "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
            "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int",
            "interface", "internal", "is", "lock", "long", "namespace", "new", "null",
            "object", "operator", "out", "override", "params", "private", "protected",
            "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof",
            "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
            "virtual", "volatile", "void", "while"
        };

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

        return PropertyName(name);
    }


    /// <summary>
    /// Converts a given string into a valid C# identifier.
    /// </summary>
    /// <remarks>
    /// This method is based on rules defined in <a href="http://msdn.microsoft.com/en-us/library/aa664670.aspx">C# language specification</a>. 
    /// It removes white space characters and concatenates words using camelCase notation. If the resulting string is 
    /// a C# keyword, the the method prepends it with the @ symbol to change it into a literal C# identifier. You can override this method
    /// in your template to replace the default implementation.
    /// </remarks>
    public virtual string Identifier(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        var builder = new StringBuilder();

        char c = '\x0000';     // current character within name
        int i = 0;             // current index within name

        // Skip invalid characters from the beginning of the name 
        while (i < name.Length)
        {
            c = name[i++];

            // First character must be a letter or _
            if (char.IsLetter(c) || c == '_')
            {
                break;
            }
        }

        if (i <= name.Length)
        {
            builder.Append(c);
        }

        bool capitalizeNext = false;

        // Strip invalid characters from the remainder of the name and convert it to camelCase
        while (i < name.Length)
        {
            c = name[i++];

            // Subsequent character can be a letter, a digit, combining, connecting or formatting character
            UnicodeCategory category = char.GetUnicodeCategory(c);
            if (!char.IsLetterOrDigit(c) &&
                category != UnicodeCategory.SpacingCombiningMark &&
                category != UnicodeCategory.ConnectorPunctuation &&
                category != UnicodeCategory.Format)
            {
                capitalizeNext = true;
                continue;
            }

            if (capitalizeNext)
            {
                c = char.ToUpperInvariant(c);
                capitalizeNext = false;
            }

            builder.Append(c);
        }

        string identifier = builder.ToString();

        // If identifier is a reserved C# keyword
        if (ReservedKeywords.Contains(identifier))
        {
            // Convert it to literal identifer
            return "@" + identifier;
        }

        return identifier;
    }

    /// <summary>
    /// Converts a given string to a valid C# property name using PascalCase notation.
    /// </summary>
    /// <remarks>
    /// This method converts the first letter of the given string to upper case and calls the <see cref="Identifier"/> method to ensure that it's valid.
    /// You can override this method in your template to replace the default implementation.
    /// </remarks>
    public virtual string PropertyName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        name = name.Trim();
        name = char.ToUpperInvariant(name[0]) + name.Substring(1);
        var result = this.Identifier(name);
        if (double.TryParse(result, out var _))
            result = string.Empty;
        return result;
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

    public abstract List<TypeDefinition> Parse(TextReader reader, GeneratorExecutionContext context, TypeDefinition rootType, List<TypeDefinition> types);
}
