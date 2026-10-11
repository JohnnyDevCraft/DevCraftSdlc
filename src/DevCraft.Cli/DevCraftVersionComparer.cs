using System.Text.RegularExpressions;

namespace DevCraft.Cli;

public static partial class DevCraftVersionComparer
{
    public static DevCraftVersionComparison CompareRepositoryToCurrent(string? repositoryVersion, string currentVersion)
    {
        if (!TryParse(repositoryVersion, out ParsedVersion repository) ||
            !TryParse(currentVersion, out ParsedVersion current))
        {
            return DevCraftVersionComparison.MissingOrInvalid;
        }

        return Compare(repository, current) < 0
            ? DevCraftVersionComparison.Older
            : DevCraftVersionComparison.CurrentOrNewer;
    }

    private static int Compare(ParsedVersion left, ParsedVersion right)
    {
        int core = left.Major.CompareTo(right.Major);

        if (core != 0)
        {
            return core;
        }

        core = left.Minor.CompareTo(right.Minor);

        if (core != 0)
        {
            return core;
        }

        core = left.Patch.CompareTo(right.Patch);

        return core != 0
            ? core
            : ComparePrerelease(left.Prerelease, right.Prerelease);
    }

    private static int ComparePrerelease(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) && string.IsNullOrWhiteSpace(right))
        {
            return 0;
        }

        if (string.IsNullOrWhiteSpace(left))
        {
            return 1;
        }

        if (string.IsNullOrWhiteSpace(right))
        {
            return -1;
        }

        string[] leftParts = left.Split('.', '-');
        string[] rightParts = right.Split('.', '-');
        int count = Math.Max(leftParts.Length, rightParts.Length);

        for (int index = 0; index < count; index++)
        {
            if (index >= leftParts.Length)
            {
                return -1;
            }

            if (index >= rightParts.Length)
            {
                return 1;
            }

            bool leftNumber = int.TryParse(leftParts[index], out int leftValue);
            bool rightNumber = int.TryParse(rightParts[index], out int rightValue);
            int comparison = (leftNumber, rightNumber) switch
            {
                (true, true) => leftValue.CompareTo(rightValue),
                (true, false) => -1,
                (false, true) => 1,
                _ => string.Compare(leftParts[index], rightParts[index], StringComparison.OrdinalIgnoreCase),
            };

            if (comparison != 0)
            {
                return comparison;
            }
        }

        return 0;
    }

    private static bool TryParse(string? value, out ParsedVersion version)
    {
        version = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        Match match = VersionRegex().Match(value.Trim().TrimStart('v'));

        if (!match.Success)
        {
            return false;
        }

        version = new ParsedVersion(
            int.Parse(match.Groups["major"].Value),
            int.Parse(match.Groups["minor"].Value),
            int.Parse(match.Groups["patch"].Value),
            match.Groups["pre"].Success ? match.Groups["pre"].Value : null);

        return true;
    }

    [GeneratedRegex(@"^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-(?<pre>[0-9A-Za-z.-]+))?(?:\+.*)?$")]
    private static partial Regex VersionRegex();
}
