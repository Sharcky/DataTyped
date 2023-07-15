using Microsoft.CodeAnalysis;

namespace DataTyped.Model;

public abstract record DataSourceType()
{
    public record Http     (string Uri)      : DataSourceType();

    public record LocalFile(string FileName) : DataSourceType();

    public record Code     (SyntaxNode Node) : DataSourceType();

    public record Inferred (string Location) : DataSourceType();
}

public abstract record Format()
{
    public record Json() : Format();

    public record Csv()  : Format();

    public record Xml()  : Format();

    public record Yaml() : Format();
}

public record DataSource(DataSourceType Type, Format Format);
