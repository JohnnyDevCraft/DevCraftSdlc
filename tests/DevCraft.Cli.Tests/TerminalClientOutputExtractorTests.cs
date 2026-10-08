using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class TerminalClientOutputExtractorTests
{
    [Fact]
    public void ExtractReadsCodexFatalErrorWithoutLettingWarningsOverrideIt()
    {
        SupportedTerminalClient codex = Assert.Single(SupportedTerminalClientCatalog.Create(), client => client.Slug == "codex");
        const string output = """
        {"type":"thread.started","thread_id":"00000000-0000-0000-0000-000000000000"}
        {"type":"item.completed","item":{"type":"error","message":"Ignoring unknown feature: ultrafast_mode enterprise requirements"}}
        {"type":"turn.started"}
        {"type":"error","message":"status 400 invalid_request_error: The gpt-5.3-codex model is not supported when using Codex with a ChatGPT account"}
        {"type":"turn.failed","error":{"message":"The gpt-5.3-codex model is not supported when using Codex with a ChatGPT account"}}
        """;

        TerminalClientOutput result = TerminalClientOutputExtractor.Extract(output, codex);

        Assert.Contains(result.Errors, error => error.Contains("not supported when using Codex with a ChatGPT account", StringComparison.Ordinal));
        Assert.Contains(result.Warnings, warning => warning.Contains("Ignoring unknown feature", StringComparison.Ordinal));
    }

    [Fact]
    public void ExtractReadsClaudeJsonResult()
    {
        SupportedTerminalClient claude = Assert.Single(SupportedTerminalClientCatalog.Create(), client => client.Slug == "claude-code");
        const string output = """
        {
          "type": "result",
          "subtype": "success",
          "result": "{\"projectProfile\":{\"name\":\"Claude Project\",\"purpose\":\"Parse Claude output.\",\"description\":\"Claude returned JSON text.\"},\"projects\":[],\"aiDrivenSdlc\":{\"detected\":false,\"name\":null}}"
        }
        """;

        TerminalClientOutput result = TerminalClientOutputExtractor.Extract(output, claude);

        Assert.Contains("Claude Project", result.Response);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ExtractReadsClaudeJsonError()
    {
        SupportedTerminalClient claude = Assert.Single(SupportedTerminalClientCatalog.Create(), client => client.Slug == "claude-code");
        const string output = """
        {
          "type": "error",
          "error": {
            "message": "Claude Code login required before non-interactive use."
          }
        }
        """;

        TerminalClientOutput result = TerminalClientOutputExtractor.Extract(output, claude);

        Assert.Contains(result.Errors, error => error.Contains("login required", StringComparison.Ordinal));
    }
}
