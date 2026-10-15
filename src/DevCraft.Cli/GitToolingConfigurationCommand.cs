namespace DevCraft.Cli;

// Configure DevCraft > Configure GIT Tooling: device Git identity, branch variables, and the branch template.
public static class GitToolingConfigurationCommand
{
    public const string ConfigureGitTooling = "Configure GIT Tooling";
    private const string SetupIdentity = "Setup GIT Configuration (Email & Name)";
    private const string SetupVariables = "Setup Branch Variables";
    private const string SetupTemplate = "Setup Branch Template";
    private const string AddVariable = "Add Variable";
    private const string EditVariable = "Edit Variable";
    private const string RemoveVariable = "Remove Variable";
    private const string Back = "Back";
    private const string Separator = " = ";

    public static void Run(StartupContext context, IConsoleInteraction console, IGitRunner git)
    {
        string? notice = null;

        while (true)
        {
            string selected = console.Select(WithNotice(notice, ConfigureGitTooling), [SetupIdentity, SetupVariables, SetupTemplate, Back]);

            try
            {
                notice = selected switch
                {
                    SetupIdentity => SetIdentity(context, console, git),
                    SetupVariables => ManageVariables(context, console),
                    SetupTemplate => SetTemplate(context, console),
                    _ => null,
                };
            }
            catch (InvalidOperationException exception)
            {
                notice = exception.Message;
            }

            if (selected == Back)
            {
                return;
            }
        }
    }

    private static string SetIdentity(StartupContext context, IConsoleInteraction console, IGitRunner git)
    {
        string email = ReadGlobal(context, git, "user.email");
        string name = ReadGlobal(context, git, "user.name");
        FieldEditResult edit = console.EditFields(new FieldEditOptions(
            "Setup GIT Configuration",
            [new FieldEditField("Email", "Email", email, Required: true), new FieldEditField("Name", "Name", name, Required: true)]));

        if (!edit.Saved)
        {
            return "Git configuration was not changed.";
        }

        string newEmail = Value(edit, "Email");
        string newName = Value(edit, "Name");

        if (newEmail.Length == 0 || newName.Length == 0)
        {
            return "Email and name are required. Git configuration was not changed.";
        }

        foreach ((string key, string value) in new[] { ("user.email", newEmail), ("user.name", newName) })
        {
            TerminalProcessResult result = git.Run(context.CurrentDirectory, ["config", "--global", key, value]);

            if (result.ExitCode != 0)
            {
                return $"Could not set {key}: {result.StandardError.Trim()}";
            }
        }

        return $"Git identity set: {newName} <{newEmail}>.";
    }

    private static string? ManageVariables(StartupContext context, IConsoleInteraction console)
    {
        string? notice = null;

        while (true)
        {
            List<KeyValuePair<string, string>> variables = ProfileConfigurationReader.Read(context.ProfileDirectory).GitTooling.BranchVariables.ToList();
            string title = WithNotice(
                notice,
                $"Branch Variables{Environment.NewLine}Built-in tokens: {{WorkItem}}, {{Title}}, {{FeatureType}}. A value of {BranchNameResolver.InputValue} is asked for when a branch is created.");
            List<string> choices = [.. variables.Select(Label), AddVariable, Back];
            string selected = console.Select(title, choices);

            if (selected == Back)
            {
                return null;
            }

            if (selected == AddVariable)
            {
                notice = EditVariableForm(context, console, variables, null);
                continue;
            }

            KeyValuePair<string, string> variable = variables.First(pair => Label(pair) == selected);
            string action = console.Select(selected, [EditVariable, RemoveVariable, Back]);

            if (action == EditVariable)
            {
                notice = EditVariableForm(context, console, variables, variable);
            }
            else if (action == RemoveVariable)
            {
                SaveVariables(context, variables.Where(pair => pair.Key != variable.Key));
                notice = $"Removed branch variable {variable.Key}.";
            }
        }
    }

    private static string EditVariableForm(
        StartupContext context,
        IConsoleInteraction console,
        List<KeyValuePair<string, string>> variables,
        KeyValuePair<string, string>? existing)
    {
        FieldEditResult edit = console.EditFields(new FieldEditOptions(
            existing is null ? "Add Branch Variable" : "Edit Branch Variable",
            [
                new FieldEditField("Name", "Name", existing?.Key ?? string.Empty, Required: true),
                new FieldEditField("Value", $"Value ({BranchNameResolver.InputValue} to ask each time)", existing?.Value ?? string.Empty, Required: true),
            ]));

        if (!edit.Saved)
        {
            return "Branch variables were not changed.";
        }

        string name = Value(edit, "Name");
        string value = Value(edit, "Value");

        if (name.Length == 0 || value.Length == 0)
        {
            return "Name and value are required. Branch variables were not changed.";
        }

        if (name.Any(character => char.IsWhiteSpace(character) || character is '{' or '}'))
        {
            return "Variable names cannot contain spaces or braces. Branch variables were not changed.";
        }

        if (BranchNameResolver.IsBuiltIn(name))
        {
            return $"{name} is a built-in token and is entered when a branch is created. Branch variables were not changed.";
        }

        if (variables.Any(pair => pair.Key.Equals(name, StringComparison.OrdinalIgnoreCase) && pair.Key != existing?.Key))
        {
            return $"A variable named {name} already exists. Branch variables were not changed.";
        }

        KeyValuePair<string, string> updated = new(name, value);
        SaveVariables(
            context,
            existing is null
                ? [.. variables, updated]
                : variables.Select(pair => pair.Key == existing.Value.Key ? updated : pair));

        return $"Saved branch variable {name}{Separator}{value}.";
    }

    private static string SetTemplate(StartupContext context, IConsoleInteraction console)
    {
        DevCraftProfileConfiguration profile = ProfileConfigurationReader.Read(context.ProfileDirectory);
        ValueEditResult edit = console.EditValue("Setup Branch Template", "Branch template", profile.GitTooling.BranchTemplate, required: true);
        string template = edit.Value.Trim();

        if (!edit.Saved)
        {
            return "Branch template was not changed.";
        }

        if (template.Length == 0)
        {
            return "A branch template is required. Branch template was not changed.";
        }

        DevCraftProfileConfigurationWriter.Write(
            context.ProfileDirectory,
            profile with { GitTooling = profile.GitTooling with { BranchTemplate = template } });

        List<string> asked = BranchNameResolver.Tokens(template)
            .Where(token => !BranchNameResolver.IsBuiltIn(token))
            .Where(token =>
            {
                string? value = profile.GitTooling.BranchVariables.FirstOrDefault(pair => pair.Key.Equals(token, StringComparison.OrdinalIgnoreCase)).Value;
                return value is null || value.Equals(BranchNameResolver.InputValue, StringComparison.OrdinalIgnoreCase);
            })
            .ToList();

        return asked.Count == 0
            ? $"Branch template set: {template}"
            : $"Branch template set: {template}. These tokens will be asked for when a branch is created: {string.Join(", ", asked)}";
    }

    private static void SaveVariables(StartupContext context, IEnumerable<KeyValuePair<string, string>> variables)
    {
        DevCraftProfileConfiguration profile = ProfileConfigurationReader.Read(context.ProfileDirectory);
        Dictionary<string, string> updated = new(StringComparer.OrdinalIgnoreCase);

        foreach ((string name, string value) in variables)
        {
            updated[name] = value;
        }

        DevCraftProfileConfigurationWriter.Write(
            context.ProfileDirectory,
            profile with { GitTooling = profile.GitTooling with { BranchVariables = updated } });
    }

    private static string ReadGlobal(StartupContext context, IGitRunner git, string key)
    {
        TerminalProcessResult result = git.Run(context.CurrentDirectory, ["config", "--global", key]);

        return result.ExitCode == 0 ? result.StandardOutput.Trim() : string.Empty;
    }

    private static string Value(FieldEditResult edit, string key)
    {
        return edit.Values.TryGetValue(key, out string? value) ? value.Trim() : string.Empty;
    }

    private static string Label(KeyValuePair<string, string> variable)
    {
        return $"{variable.Key}{Separator}{variable.Value}";
    }

    private static string WithNotice(string? notice, string title)
    {
        return notice is null ? title : $"{notice}{Environment.NewLine}{Environment.NewLine}{title}";
    }
}
