namespace DevCraft.Cli;

using System.Diagnostics;

public sealed class FeatureAiSessionLauncher : IFeatureAiSessionLauncher, IDevCraftAiSessionLauncher
{
    public void Launch(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        AgentHandoff handoff,
        string instruction)
    {
        LaunchInternal(context, projectConfiguration, null, handoff, instruction);
    }

    public void Launch(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        DevCraftFeature feature,
        AgentHandoff handoff,
        string instruction)
    {
        LaunchInternal(context, projectConfiguration, feature, handoff, instruction);
    }

    private static void LaunchInternal(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        DevCraftFeature? feature,
        AgentHandoff handoff,
        string instruction)
    {
        if (Environment.GetEnvironmentVariable("DEVCRAFT_DISABLE_CLIENT_LAUNCH") == "1")
        {
            return;
        }

        string prompt = AgentHandoffPromptBuilder.Build(context, projectConfiguration, feature, instruction, handoff.StartingPrompt);
        SupportedTerminalClient client = handoff.Client;

        ProcessStartInfo startInfo = new()
        {
            FileName = client.Session.BinaryPath,
            UseShellExecute = false,
            WorkingDirectory = context.CurrentDirectory,
        };

        foreach (string argument in client.Session.Arguments)
        {
            startInfo.ArgumentList.Add(argument
                .Replace("{workingDirectory}", context.CurrentDirectory, StringComparison.Ordinal)
                .Replace("{prompt}", prompt, StringComparison.Ordinal));
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start {client.Name}.");
        process.WaitForExit();
    }
}
