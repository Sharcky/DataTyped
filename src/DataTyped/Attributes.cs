using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text.Json;

namespace DataTyped.Generator;

[AttributeUsage(AttributeTargets.Class)]
public class JsonTypeAttribute : Attribute
{
    public string? Url { get; set; }
    public string? Filename { get; set; }
    public string? Name { get; set; }
}

[AttributeUsage(AttributeTargets.Class)]
public class CsvTypeAttribute : Attribute
{
    public string? Url { get; set; }
    public string? Filename { get; set; }
    public string? Name { get; set; }
}

[AttributeUsage(AttributeTargets.Class)]
public class XmlTypeAttribute : Attribute
{
    public string? Url { get; set; }
    public string? Filename { get; set; }
    public string? Name { get; set; }
}

public class YamlTypeAttribute : Attribute
{
    public string? Url { get; set; }
    public string? Filename { get; set; }
    public string? Name { get; set; }
}

public static class Json
{
    public static async Task<JsonDocument> Get(string urlOrFilePath)
    {
        var content = await Helpers.GetContent(urlOrFilePath);
        var result = JsonDocument.Parse(content);
        return result;
    }

    public static async Task<T?> Get<T>(string urlOrFilePath)
    {
        var content = await Helpers.GetContent(urlOrFilePath);
       
        var result = JsonSerializer.Deserialize<T>(content);
        return result;
    }
}

public static class Csv
{
    public static async Task<CsvReader> Get(string urlOrFilePath)
    {
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture);
        if (Helpers.IsValidUrl(urlOrFilePath))
        {
            var content = await Helpers.HttpGet(urlOrFilePath);
            var reader = new StringReader(content);
            return new CsvReader(reader,cfg);
        }
        else if (Helpers.IsValidFilePath(urlOrFilePath))
        {
            var reader = new StreamReader(urlOrFilePath);
            return new CsvReader(reader, cfg);
        }
        else
            throw new InvalidOperationException($"Cannot get content from {urlOrFilePath}. The path is neither a valid HTTP/HTTPS URL nor a valid file path on disk.");
    }

    public static async Task<IEnumerable<T>> Get<T>(string urlOrFilePath)
    {
        var reader = await Get(urlOrFilePath);
        return reader.GetRecords<T>();
    }
}

file static class Helpers
{
    private static readonly HttpClient httpClient = new();

    public static async Task<string> GetContent(string urlOrFilePath)
    {
        if (IsValidUrl(urlOrFilePath))
        {
            return await HttpGet(urlOrFilePath);
        }
        else if (IsValidFilePath(urlOrFilePath))
        {
            return File.ReadAllText(urlOrFilePath);
        }
        else
            throw new InvalidOperationException($"Cannot get content from {urlOrFilePath}. The path is neither a valid HTTP/HTTPS URL nor a valid file path on disk.");
    }

    public static async Task<string> HttpGet(string url)
    {
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        return body;
    }

    public static bool IsValidUrl(string url)
    {
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = "https://" + url;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult))
            return false;

        if (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps)
            return false;

        if (uriResult.Host.Replace("www.", "").Split('.').Count() <= 1)
            return false;

        if (uriResult.HostNameType != UriHostNameType.Dns)
            return false;

        if (uriResult.Host.Length <= uriResult.Host.LastIndexOf(".") + 1)
            return false;

        if (url.Length > 256)
            return false;

        return true;
    }

    public static bool IsValidFilePath(string filePath)
    {
        return File.Exists(filePath);
    }
}

public static class GeneratedAttributes
{
    public const string Text =
@$"
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text.Json;

namespace DataTyped;

[AttributeUsage(AttributeTargets.Class)]
public class JsonTypeAttribute : Attribute
{{
    public string? Url {{ get; set; }}
    public string? Filename {{ get; set; }}
    public string? Name {{ get; set; }}
}}

[AttributeUsage(AttributeTargets.Class)]
public class CsvTypeAttribute : Attribute
{{
    public string? Url {{ get; set; }}
    public string? Filename {{ get; set; }}
    public string? Name {{ get; set; }}
}}

[AttributeUsage(AttributeTargets.Class)]
public class XmlTypeAttribute : Attribute
{{
    public string? Url {{ get; set; }}
    public string? Filename {{ get; set; }}
    public string? Name {{ get; set; }}
}}

public class YamlTypeAttribute : Attribute
{{
    public string? Url {{ get; set; }}
    public string? Filename {{ get; set; }}
    public string? Name {{ get; set; }}
}}

[AttributeUsage(AttributeTargets.Property)]
public class PropertyAttribute : Attribute
{{
    public PropertyAttribute(string name)
    {{
        Name = name;
    }}

    public PropertyAttribute(int order)
    {{
        Order = order;
    }}

    public string? Name {{ get; }}
    public int? Order {{ get; }}
}}

public static class Json
{{
    public static async Task<JsonDocument> Get(string urlOrFilePath)
    {{
        var content = await Helpers.GetContent(urlOrFilePath);
        var result = JsonDocument.Parse(content);
        return result;
    }}

    public static async Task<T?> Get<T>(string urlOrFilePath)
    {{
        var content = await Helpers.GetContent(urlOrFilePath);
       
        var result = JsonSerializer.Deserialize<T>(content);
        return result;
    }}
}}

public static class Csv
{{
    public static async Task<CsvReader> Get(string urlOrFilePath)
    {{
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture);
        if (Helpers.IsValidUrl(urlOrFilePath))
        {{
            var content = await Helpers.HttpGet(urlOrFilePath);
            var reader = new StringReader(content);
            return new CsvReader(reader,cfg);
        }}
        else if (Helpers.IsValidFilePath(urlOrFilePath))
        {{
            var reader = new StreamReader(urlOrFilePath);
            return new CsvReader(reader, cfg);
        }}
        else
            throw new InvalidOperationException($""Cannot get content from {{urlOrFilePath}}. The path is neither a valid HTTP/HTTPS URL nor a valid file path on disk."");
    }}

    public static async Task<IEnumerable<T>> Get<T>(string urlOrFilePath)
    {{
        var reader = await Get(urlOrFilePath);
        return reader.GetRecords<T>();
    }}
}}

file static class Helpers
{{
    private static readonly HttpClient httpClient = new();

    public static async Task<string> GetContent(string urlOrFilePath)
    {{
        if (IsValidUrl(urlOrFilePath))
        {{
            return await HttpGet(urlOrFilePath);
        }}
        else if (IsValidFilePath(urlOrFilePath))
        {{
            return File.ReadAllText(urlOrFilePath);
        }}
        else
            throw new InvalidOperationException($""Cannot get content from {{urlOrFilePath}}. The path is neither a valid HTTP/HTTPS URL nor a valid file path on disk."");
    }}

    public static async Task<string> HttpGet(string url)
    {{
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        return body;
    }}

    public static bool IsValidUrl(string url)
    {{
        if (!url.StartsWith(""http://"") && !url.StartsWith(""https://""))
        {{
            url = ""https://"" + url;
        }}

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult))
            return false;

        if (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps)
            return false;

        if (uriResult.Host.Replace(""www."", """").Split('.').Count() <= 1)
            return false;

        if (uriResult.HostNameType != UriHostNameType.Dns)
            return false;

        if (uriResult.Host.Length <= uriResult.Host.LastIndexOf(""."") + 1)
            return false;

        if (url.Length > 256)
            return false;

        return true;
    }}

    public static bool IsValidFilePath(string filePath)
    {{
        return File.Exists(filePath);
    }}
}}
";

}