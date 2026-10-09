using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class CliLogoRendererTests
{
    [Fact]
    public void CreateIncludesDevCraftLogoText()
    {
        CliLogo logo = CliLogoRenderer.Create();

        string output = logo.ToPlainText();

        Assert.Contains("_", output);
        Assert.Contains("____", output);
        Assert.NotEmpty(logo.Lines);
    }

    [Fact]
    public void CreateIncludesCopyrightLine()
    {
        CliLogo logo = CliLogoRenderer.Create();

        string output = logo.ToPlainText();

        Assert.Contains("© 2026 Xelseor LLC", output);
    }

    [Fact]
    public void CreateIncludesCreatorCredit()
    {
        CliLogo logo = CliLogoRenderer.Create();

        string output = logo.ToPlainText();

        Assert.Contains("Designed and created by JohnnyDevCraft", output);
    }

    [Fact]
    public void CreateIncludesVersion()
    {
        CliLogo logo = CliLogoRenderer.Create();

        string output = logo.ToPlainText();

        Assert.Contains("Version 1.0.0-beta.9", output);
        Assert.DoesNotContain("+", logo.VersionLine);
    }

    [Fact]
    public void CreateDoesNotIncludeUnrelatedWelcomeContent()
    {
        CliLogo logo = CliLogoRenderer.Create();

        string output = logo.ToPlainText();

        Assert.DoesNotContain("status", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("next action", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dashboard", output, StringComparison.OrdinalIgnoreCase);
    }
}
