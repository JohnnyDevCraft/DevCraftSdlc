using System.Text;

namespace DevCraft.Cli;

public static class SlugGenerator
{
    public static string Create(string value)
    {
        StringBuilder builder = new();
        bool previousWasSeparator = false;

        foreach (char character in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSeparator = false;
                continue;
            }

            if (previousWasSeparator || builder.Length == 0)
            {
                continue;
            }

            builder.Append('-');
            previousWasSeparator = true;
        }

        string slug = builder.ToString().Trim('-');

        return string.IsNullOrWhiteSpace(slug) ? "item" : slug;
    }
}
