namespace DevCraft.Cli;

// Headless default-agent runs that update the repository .devcraft control files.
public static class RepositoryDocumentCommand
{
    public static readonly IReadOnlyList<string> ConstitutionFiles = ["AGENT.md", "README.md", "DISCOVERY.md", "ARCH.md", "GOV.md", "MODEL.md", "THEME.md"];
    public static readonly IReadOnlyList<string> InferFiles = ["ARCH.md", "DISCOVERY.md", "MODEL.md", "GOV.md", "THEME.md"];
    private const string ImportLabel = "SpecKit constitution import";
    private const string InferLabel = "Infer Project Details";

    public static string ImportSpecKitConstitution(StartupContext context, IConsoleInteraction console, IRepositoryDocumentAgent agent)
    {
        string constitutionPath = SpecKitConstitutionRecord.PathFor(context.CurrentDirectory);

        if (!File.Exists(constitutionPath))
        {
            return $"No SpecKit constitution found at {SpecKitConstitutionRecord.RelativePath}.";
        }

        string readmePath = Path.Combine(context.CurrentDirectory, ".devcraft", "README.md");
        string currentMd5 = SpecKitConstitutionRecord.ComputeMd5(constitutionPath);
        string? recordedMd5 = SpecKitConstitutionRecord.ReadRecordedMd5(readmePath);
        string changeNote;
        string changeNotice;

        if (recordedMd5 is null)
        {
            changeNote = "This is the first import of this constitution into DevCraft.";
            changeNotice = "This was the first import.";
        }
        else if (!recordedMd5.Equals(currentMd5, StringComparison.OrdinalIgnoreCase))
        {
            changeNote = $"The constitution has changed since the last import (previous MD5 {recordedMd5}, current MD5 {currentMd5}). Update the DevCraft files to reflect what changed.";
            changeNotice = "The constitution changed since the last import.";
        }
        else
        {
            console.ShowMenuShell();
            string again = console.Select(
                $"The SpecKit constitution has already been imported (MD5 {currentMd5}).{Environment.NewLine}{Environment.NewLine}Import it again anyway?",
                ["No", "Yes"]);

            if (again != "Yes")
            {
                return $"The SpecKit constitution is already imported and unchanged (MD5 {currentMd5}). No agent was run.";
            }

            changeNote = "The constitution is unchanged since the last import. The operator asked to apply it again, so reconcile the DevCraft files with it.";
            changeNotice = "The constitution is unchanged since the last import.";
        }

        string prompt = RepositoryDocumentPromptBuilder.BuildConstitutionImport(context.CurrentDirectory, constitutionPath, ConstitutionFiles, changeNote);
        (RepositoryDocumentSyncResult? result, string notice) = Run(context, console, agent, ImportLabel, "Reading the SpecKit constitution", prompt, ConstitutionFiles);

        if (result is null)
        {
            return notice;
        }

        SpecKitConstitutionRecord.Record(readmePath, currentMd5);

        return $"{notice} {changeNotice}";
    }

    public static string InferProjectDetails(StartupContext context, IConsoleInteraction console, IRepositoryDocumentAgent agent)
    {
        string constitutionPath = SpecKitConstitutionRecord.PathFor(context.CurrentDirectory);
        string specsDirectory = Path.Combine(context.CurrentDirectory, "specs");
        string prompt = RepositoryDocumentPromptBuilder.BuildInferProjectDetails(
            context.CurrentDirectory,
            File.Exists(constitutionPath) ? constitutionPath : null,
            Directory.Exists(specsDirectory) ? specsDirectory : null,
            InferFiles);

        return Run(context, console, agent, InferLabel, "Inferring project details", prompt, InferFiles).Notice;
    }

    private static (RepositoryDocumentSyncResult? Result, string Notice) Run(
        StartupContext context,
        IConsoleInteraction console,
        IRepositoryDocumentAgent agent,
        string label,
        string activity,
        string prompt,
        IReadOnlyList<string> allowedFiles)
    {
        SupportedTerminalClient client = DefaultTerminalClient.Resolve(context);
        RepositoryDocumentSyncResult result;

        try
        {
            string response = console.RunStatus($"{activity} with {client.Name}...", () => agent.Run(context, client, prompt));
            result = RepositoryDocumentSync.Apply(response, Path.Combine(context.CurrentDirectory, ".devcraft"), allowedFiles);
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return (null, $"{label} failed: {exception.Message}");
        }

        if (!result.Success)
        {
            return (null, $"{label} failed: {result.Summary}");
        }

        List<string> parts = [$"{label} complete."];
        parts.Add(result.UpdatedFiles.Count == 0
            ? "No DevCraft files changed; they already match."
            : $"Updated {string.Join(", ", result.UpdatedFiles)}.");

        if (result.SkippedFiles.Count > 0)
        {
            parts.Add($"Ignored files outside the allowed list: {string.Join(", ", result.SkippedFiles)}.");
        }

        if (result.Summary.Length > 0)
        {
            parts.Add(result.Summary);
        }

        return (result, string.Join(' ', parts));
    }
}
