namespace DevCraft.Cli;

public sealed record CliLogoLine(string DevSegment, string CraftSegment)
{
    public string ToPlainText()
    {
        return DevSegment + CraftSegment;
    }
}

