using System.Text;
using System.Text.RegularExpressions;

namespace DevCraft.Cli;

// Builds branch names from a template such as {adUser}/{FeatureType}/{WorkItem}-{Title}.
// Token names match case-insensitively, and each substituted value is reduced to a ref-safe segment.
public static partial class BranchNameResolver
{
    public const string WorkItemToken = "WorkItem";
    public const string TitleToken = "Title";
    public const string FeatureTypeToken = "FeatureType";
    public const string InputValue = "INPUT";
    public const int TitleMaxLength = 55;

    public static readonly IReadOnlyList<string> BuiltInTokens = [WorkItemToken, TitleToken, FeatureTypeToken];

    public static IReadOnlyList<string> Tokens(string template)
    {
        List<string> tokens = [];

        foreach (Match match in TokenPattern().Matches(template))
        {
            string token = match.Groups[1].Value;

            if (!tokens.Contains(token, StringComparer.OrdinalIgnoreCase))
            {
                tokens.Add(token);
            }
        }

        return tokens;
    }

    public static bool IsBuiltIn(string token)
    {
        return BuiltInTokens.Contains(token, StringComparer.OrdinalIgnoreCase);
    }

    public static BranchNameResolution Resolve(string template, IReadOnlyDictionary<string, string> values)
    {
        List<string> unresolved = [];
        string branchName = TokenPattern().Replace(template, match =>
        {
            string token = match.Groups[1].Value;
            string? value = values.FirstOrDefault(pair => pair.Key.Equals(token, StringComparison.OrdinalIgnoreCase)).Value;

            if (value is null)
            {
                if (!unresolved.Contains(token, StringComparer.OrdinalIgnoreCase))
                {
                    unresolved.Add(token);
                }

                return match.Value;
            }

            return token.Equals(TitleToken, StringComparison.OrdinalIgnoreCase)
                ? Sanitize(value, TitleMaxLength)
                : Sanitize(value);
        });

        return new BranchNameResolution(branchName, unresolved);
    }

    public static string Sanitize(string value, int? maxLength = null)
    {
        string text = value.Trim().Replace("@{", string.Empty, StringComparison.Ordinal);
        text = SeparatorPattern().Replace(text, "-");

        StringBuilder builder = new(text.Length);

        foreach (char character in text)
        {
            if (!char.IsControl(character) && !"~^:?*[]{}".Contains(character))
            {
                builder.Append(character);
            }
        }

        text = DotRunPattern().Replace(builder.ToString(), ".");
        text = DashRunPattern().Replace(text, "-");
        text = text.Trim('-', '.');

        if (maxLength is int max && text.Length > max)
        {
            text = text[..max].TrimEnd('-', '.');
        }

        return text;
    }

    [GeneratedRegex(@"\{([^{}]+)\}")]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"[\s/\\]+")]
    private static partial Regex SeparatorPattern();

    [GeneratedRegex(@"\.{2,}")]
    private static partial Regex DotRunPattern();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex DashRunPattern();
}
