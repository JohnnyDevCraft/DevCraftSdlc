using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class TerminalGuiHeaderTests
{
    private static readonly CliLogo SampleLogo = new(
        [new CliLogoLine(" ____ ", " ____ ")],
        "© 2026 Xelseor LLC",
        "Designed and created by JohnnyDevCraft",
        "Version 1.0.0-beta.17 | ~/Source/repos/DevCraft | master");

    [Fact]
    public void HeaderModeIsFullAtThirtyRows()
    {
        Assert.Equal(TerminalGuiHeaderMode.Full, TerminalGuiHeader.ModeFor(30));
        Assert.Equal(TerminalGuiHeaderMode.Full, TerminalGuiHeader.ModeFor(60));
    }

    [Fact]
    public void HeaderModeIsCompactBelowThirtyRows()
    {
        Assert.Equal(TerminalGuiHeaderMode.Compact, TerminalGuiHeader.ModeFor(29));
        Assert.Equal(TerminalGuiHeaderMode.Compact, TerminalGuiHeader.ModeFor(14));
    }

    [Fact]
    public void CompactHeaderIsSingleLineWithVersionAndPath()
    {
        IReadOnlyList<string> lines = TerminalGuiHeader.Lines(SampleLogo, TerminalGuiHeaderMode.Compact);

        string line = Assert.Single(lines);
        Assert.Equal("DevCraft | Version 1.0.0-beta.17 | ~/Source/repos/DevCraft | master", line);
    }

    [Fact]
    public void FullHeaderIncludesLogoCopyrightCreatorAndVersion()
    {
        IReadOnlyList<string> lines = TerminalGuiHeader.Lines(SampleLogo, TerminalGuiHeaderMode.Full);

        Assert.Equal(4, lines.Count);
        Assert.Equal(SampleLogo.VersionLine, lines[^1]);
    }
}
