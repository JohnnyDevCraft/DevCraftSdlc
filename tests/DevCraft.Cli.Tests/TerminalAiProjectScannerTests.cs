using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class TerminalAiProjectScannerTests
{
    [Fact]
    public void BuildFailureMessageIncludesExitCodeCapturedStreamsAndCodexCacheHint()
    {
        TerminalProcessResult result = new(
            7,
            """
            {"type":"item.completed","item":{"type":"agent_message","text":"model list recovered, but later scan failed"}}
            """,
            """
            2026-10-08T17:12:55.687430Z ERROR codex_models_manager::cache: failed to load models cache: missing field 'base_instructions' at line 132 column 5
            fatal scan detail
            """);
        TerminalClientOutput output = new("model list recovered, but later scan failed", [], []);
        SupportedTerminalClient codex = Assert.Single(SupportedTerminalClientCatalog.Create(), client => client.Slug == "codex");

        string message = TerminalAiProjectScanner.BuildFailureMessage("Codex", codex, result, output);

        Assert.Contains("Codex scan failed with exit code 7.", message);
        Assert.Contains("stderr:", message);
        Assert.Contains("missing field 'base_instructions'", message);
        Assert.Contains("fatal scan detail", message);
        Assert.Contains("stdout: model list recovered, but later scan failed", message);
        Assert.Contains("refresh only the model cache", message);
    }

    [Fact]
    public void BuildStartFailureMessageExplainsMissingBinary()
    {
        string message = TerminalAiProjectScanner.BuildStartFailureMessage("Claude Code", "claude", "No such file or directory");

        Assert.Contains("Claude Code scan could not start 'claude'.", message);
        Assert.Contains("installed and available on PATH", message);
        Assert.Contains("No such file or directory", message);
    }

    [Fact]
    public void BuildFailureMessageShowsUnsupportedCodexModelHint()
    {
        TerminalProcessResult result = new(1, "", "");
        TerminalClientOutput output = new(
            "",
            ["The gpt-5.3-codex model is not supported when using Codex with a ChatGPT account"],
            ["Ignoring unknown feature: ultrafast_mode enterprise requirements"]);
        SupportedTerminalClient codex = Assert.Single(SupportedTerminalClientCatalog.Create(), client => client.Slug == "codex");

        string message = TerminalAiProjectScanner.BuildFailureMessage("Codex", codex, result, output);

        Assert.Contains("Provider error:", message);
        Assert.Contains("gpt-5.3-codex", message);
        Assert.Contains("Provider warning:", message);
        Assert.Contains("Change the Codex model", message);
    }

    [Fact]
    public void BuildFailureMessageCanReportProviderErrorEvenWithSuccessfulExit()
    {
        TerminalProcessResult result = new(0, "", "");
        TerminalClientOutput output = new(
            "{\"projectProfile\":{\"name\":\"Should Not Trust\",\"purpose\":\"Ignored.\",\"description\":\"Ignored.\"},\"projects\":[],\"aiDrivenSdlc\":{\"detected\":false,\"name\":null}}",
            ["Provider returned a structured error despite exit code 0."],
            []);
        SupportedTerminalClient claude = Assert.Single(SupportedTerminalClientCatalog.Create(), client => client.Slug == "claude-code");

        string message = TerminalAiProjectScanner.BuildFailureMessage("Claude Code", claude, result, output);

        Assert.Contains("Claude Code scan failed with exit code 0.", message);
        Assert.Contains("Provider returned a structured error", message);
    }

    [Fact]
    public void BuildInvalidResponseMessageIncludesRawOutputAndProviderError()
    {
        TerminalProcessResult result = new(0, "not-json", "stderr detail");
        TerminalClientOutput output = new(
            "not-json",
            ["Claude Code login required before non-interactive use."],
            []);
        SupportedTerminalClient claude = Assert.Single(SupportedTerminalClientCatalog.Create(), client => client.Slug == "claude-code");

        string message = TerminalAiProjectScanner.BuildInvalidResponseMessage(
            "Claude Code",
            claude,
            result,
            output,
            new InvalidOperationException("AI scan did not return valid project scan JSON."));

        Assert.Contains("Claude Code scan finished, but DevCraft could not read project scan JSON.", message);
        Assert.Contains("Parse error:", message);
        Assert.Contains("Provider error:", message);
        Assert.Contains("login required", message);
        Assert.Contains("stdout: not-json", message);
        Assert.Contains("stderr: stderr detail", message);
    }

    [Fact]
    public void CreateStartInfoUsesResolvedClaudeClientInsteadOfCodexFallback()
    {
        SupportedTerminalClient claude = SupportedTerminalClientResolver.Resolve("Claude AI", SupportedTerminalClientCatalog.Create());

        System.Diagnostics.ProcessStartInfo startInfo = TerminalAiProjectScanner.CreateStartInfo("/tmp/sample", claude, "scan prompt");

        Assert.Equal("claude", startInfo.FileName);
        Assert.Contains("--print", startInfo.ArgumentList);
        Assert.Contains("--output-format", startInfo.ArgumentList);
        Assert.Contains("json", startInfo.ArgumentList);
        Assert.DoesNotContain("exec", startInfo.ArgumentList);
    }

    [Fact]
    public void BuildFailureMessageReportsMismatchWhenSelectedClientEmitsCodexEvents()
    {
        SupportedTerminalClient claude = Assert.Single(SupportedTerminalClientCatalog.Create(), client => client.Slug == "claude-code");
        TerminalProcessResult result = new(
            1,
            """
            {"type":"thread.started","thread_id":"00000000-0000-0000-0000-000000000000"}
            {"type":"error","message":"The gpt-5.3-codex model is not supported when using Codex with a ChatGPT account."}
            """,
            "");
        TerminalClientOutput output = new(result.StandardOutput, [], []);

        string message = TerminalAiProjectScanner.BuildFailureMessage("claude-code", claude, result, output);

        Assert.Contains("Executable: claude", message);
        Assert.Contains("captured output looks like Codex JSON events", message);
    }

    [Fact]
    public void ScanParsesControlledResponse()
    {
        const string response = """
        {
          "projectProfile": {
            "name": "Sample",
            "purpose": "Demonstrate scan parsing.",
            "description": "Sample demonstrates a simple console app."
          },
          "projects": [
            {
              "name": "Sample",
              "type": ".NET console app"
            }
          ],
          "aiDrivenSdlc": {
            "detected": false,
            "name": null
          }
        }
        """;
        string? original = Environment.GetEnvironmentVariable("DEVCRAFT_AI_SCAN_RESPONSE");
        Environment.SetEnvironmentVariable("DEVCRAFT_AI_SCAN_RESPONSE", response);

        try
        {
            TerminalAiProjectScanner scanner = new();

            ProjectScanResult result = scanner.Scan("/tmp/sample", "Codex", "/tmp/profile");

            Assert.Equal("Sample", result.ProjectProfile.Name);
            DetectedProject project = Assert.Single(result.Projects);
            Assert.Equal("Sample", project.Name);
            Assert.Equal(".NET console app", project.Type);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DEVCRAFT_AI_SCAN_RESPONSE", original);
        }
    }
}
