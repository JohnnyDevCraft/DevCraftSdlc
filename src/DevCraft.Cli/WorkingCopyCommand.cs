using System.Globalization;

namespace DevCraft.Cli;

// Project Management > Remove Working Copy. DevCraft never removes working copies on its own.
public static class WorkingCopyCommand
{
    public const string RemoveWorkingCopy = "Remove Working Copy";
    private const string Back = "Back";

    public static string? Remove(StartupContext context, IConsoleInteraction console, ProjectDevCraftConfiguration configuration)
    {
        SystemCentralProject? project = SystemCentralProjectsStore.Read(context.ProfileDirectory).Projects
            .FirstOrDefault(item => item.ProjectKey.Equals(configuration.ProjectKey, StringComparison.OrdinalIgnoreCase));

        if (project is null || project.WorkingCopies.Count == 0)
        {
            return "No working copies are recorded for this project yet.";
        }

        IReadOnlyList<string> names = WorkingCopyNames.Shortest(
            project.WorkingCopies.Select(copy => copy.Path).ToList(),
            project.RepositoryName);
        List<string> choices = project.WorkingCopies.Select((copy, index) => Choice(copy, names[index])).ToList();

        console.ShowMenuShell();
        string selected = console.Select(RemoveWorkingCopy, [.. choices, Back]);

        if (selected == Back)
        {
            return null;
        }

        int position = choices.IndexOf(selected);
        WorkingCopy removed = project.WorkingCopies[position];

        if (WorkingCopyPaths.Same(removed.Path, context.CurrentDirectory))
        {
            return "This is the working copy DevCraft is running in; it cannot be removed.";
        }

        if (console.Select($"Remove working copy {removed.Path}?", ["No", "Yes"]) != "Yes")
        {
            return "Working copy was not removed.";
        }

        try
        {
            SystemCentralProjectsStore.Update(context.ProfileDirectory, index => new SystemCentralProjectsIndex(
                index.Projects
                    .Select(item => item.ProjectKey.Equals(project.ProjectKey, StringComparison.OrdinalIgnoreCase)
                        ? item with { WorkingCopies = item.WorkingCopies.Where(copy => !WorkingCopyPaths.Same(copy.Path, removed.Path)).ToList() }
                        : item)
                    .ToList()));
        }
        catch (ProjectsIndexBusyException exception)
        {
            return exception.Message;
        }

        return $"Removed working copy {names[position]} ({removed.Path}).";
    }

    private static string Choice(WorkingCopy copy, string name)
    {
        string branch = copy.Branch ?? "(none)";
        string seen = copy.LastSeen is DateTimeOffset lastSeen
            ? lastSeen.UtcDateTime.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture)
            : "unknown";
        string missing = Directory.Exists(copy.Path) ? string.Empty : " — missing";

        return $"{name} — {copy.Path} — {branch} — last seen {seen}{missing}";
    }
}
