namespace DevCraft.Cli;

public static class AgentHandoffPromptBuilder
{
    public static string Build(
        StartupContext context,
        ProjectDevCraftConfiguration projectConfiguration,
        DevCraftFeature? feature,
        string instruction,
        string startingPrompt)
    {
        string profileDirectory = context.ProfileDirectory;
        string repoConfigurationPath = string.IsNullOrWhiteSpace(projectConfiguration.ProjectKey)
            ? "none (DevCraft is not running in a project folder)"
            : ProjectDevCraftConfigurationStore.PathFor(context.CurrentDirectory);
        string featureText = feature is null
            ? "No active feature was selected for this handoff."
            : $"""
                Feature:
                - Name: {feature.Name}
                - Slug: {feature.Slug}
                - Short description: {feature.ShortDescription}
                - Folder name: {feature.FolderName}
                - Storage: {feature.StorageType}
                - Work item ID: {feature.WorkItemId ?? "None"}
                - Work item URL: {feature.ExternalReference ?? "None"}
                - Branch: {feature.Branch ?? "None"}
                """;
        string situationContext = SituationPromptContextBuilder.Build(profileDirectory);

        string prompt = $"""
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

            {situationContext}

            Operate in DevCraft mode.
            """;

        if (string.IsNullOrWhiteSpace(startingPrompt))
        {
            return prompt;
        }

        // The operator's own words go last so they are the agent's most recent instruction.
        return string.Join(
            Environment.NewLine,
            prompt,
            string.Empty,
            "Operator starting prompt:",
            startingPrompt.Trim());
    }
}
