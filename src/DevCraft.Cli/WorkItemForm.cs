namespace DevCraft.Cli;

// Work Item ID (free text, any tracker) and Work Item URL (the item's web page) as optional form fields.
// An invalid URL reopens the form with every entered value kept; empty values become null.
public static class WorkItemForm
{
    public const string WorkItemIdKey = "WorkItemId";
    public const string WorkItemUrlKey = "WorkItemUrl";

    public static IReadOnlyList<FieldEditField> Fields(string? workItemId, string? workItemUrl)
    {
        return
        [
            new FieldEditField(WorkItemIdKey, "Work Item ID", workItemId ?? string.Empty),
            new FieldEditField(WorkItemUrlKey, "Work Item URL", workItemUrl ?? string.Empty),
        ];
    }

    public static FieldEditResult Edit(IConsoleInteraction console, string title, IReadOnlyList<FieldEditField> fields)
    {
        string currentTitle = title;
        IReadOnlyList<FieldEditField> currentFields = fields;

        while (true)
        {
            FieldEditResult result = console.EditFields(new FieldEditOptions(currentTitle, currentFields));

            if (!result.Saved || Value(result, WorkItemUrlKey) is not string url || WorkItemUrl.IsValid(url))
            {
                return result;
            }

            currentTitle = $"{WorkItemUrl.InvalidMessage}{Environment.NewLine}{Environment.NewLine}{title}";
            currentFields = currentFields
                .Select(field => field with { InitialValue = result.Values.TryGetValue(field.Key, out string? entered) ? entered : field.InitialValue })
                .ToList();
        }
    }

    public static string? Value(FieldEditResult result, string key)
    {
        string value = result.Values.TryGetValue(key, out string? entered) ? entered.Trim() : string.Empty;

        return value.Length == 0 ? null : value;
    }
}
