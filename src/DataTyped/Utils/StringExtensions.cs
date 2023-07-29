namespace DataTyped.Generator;

public static class StringExtensions
{
    public static string Join(this IEnumerable<string> source, string? separator = null) =>
        string.Join(separator ?? Environment.NewLine, source);

    public static bool IsNullOrEmpty(this string? str) => string.IsNullOrEmpty(str);

    public static bool IsIn(this string str, IEnumerable<string> arr) => arr.Contains(str);

    public static bool EqualsInvariant(this string? str, string? other) =>
        str is null
        ? other is null
        : str.Equals(other, StringComparison.InvariantCulture);
}