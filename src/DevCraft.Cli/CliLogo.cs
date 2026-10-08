namespace DevCraft.Cli;

public sealed record CliLogo(
    IReadOnlyList<CliLogoLine> Lines,
    string CopyrightLine,
    string CreatorLine,
    string VersionLine)
{
    public string ToPlainText()
    {
        IEnumerable<string> logoLines = Lines.Select(line => line.ToPlainText());

        return string.Join(
            Environment.NewLine,
            logoLines
                .Append(CopyrightLine)
                .Append(CreatorLine)
                .Append(VersionLine));
    }
}
