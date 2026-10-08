namespace DevCraft.Cli;

using System.Diagnostics;

public sealed class FeatureAiSessionLauncher : IFeatureAiSessionLauncher, IDevCraftAiSessionLauncher
{
    public void Launch(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        SupportedTerminalClient client,
        string instruction)
    {
        LaunchInternal(context, projectConfiguration, null, client, instruction);
    }

    public void Launch(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        DevCraftFeature feature,
        SupportedTerminalClient client,
        string instruction)
    {
        LaunchInternal(context, projectConfiguration, feature, client, instruction);
    }

    private static void LaunchInternal(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        DevCraftFeature? feature,
        SupportedTerminalClient client,
        string instruction)
    {
        if (Environment.GetEnvironmentVariable("DEVCRAFT_DISABLE_CLIENT_LAUNCH") == "1")
        {
            return;
        }

        string prompt = BuildPrompt(context, projectConfiguration, feature, instruction);

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

    private static string BuildPrompt(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        DevCraftFeature? feature,
        string instruction)
    {
        string profileDirectory = context.ProfileDirectory;
        string repoConfigurationPath = ProjectDevCraftConfigurationStore.PathFor(context.CurrentDirectory);
        string featureText = feature is null
            ? "No active feature was selected for this handoff."
            : $"""
                Feature:
                - Name: {feature.Name}
                - Slug: {feature.Slug}
                - Short description: {feature.ShortDescription}
                - Folder name: {feature.FolderName}
                """;

        return $"""
            You are running DevCraft for this repository.

            Read and follow these files:
            - Soul: {Path.Combine(profileDirectory, "soul.md")}
            - Initialized instructions: {Path.Combine(profileDirectory, "initialized.md")}
            - DevCraft rules: {Path.Combine(profileDirectory, "DevCraft.md")}
            - Profile configuration: {Path.Combine(profileDirectory, "configure.json")}
            - Repository configuration: {repoConfigurationPath}

            Instruction: {instruction}

            Project:
            - Key: {projectConfiguration.ProjectKey}
            - Name: {projectConfiguration.ProjectProfile.Name}
            - Feature storage: {projectConfiguration.SelectedFeatureStorage ?? "Not selected"}

            {featureText}

            Operate in DevCraft mode.
            """;
    }
}
