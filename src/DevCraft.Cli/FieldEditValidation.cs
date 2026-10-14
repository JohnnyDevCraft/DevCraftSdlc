namespace DevCraft.Cli;

public static class FieldEditValidation
{
    public static FieldEditField? FirstMissingRequired(
        IEnumerable<FieldEditField> fields,
        IReadOnlyDictionary<string, string> values)
    {
        return fields.FirstOrDefault(field =>
            field.Required
            && (!values.TryGetValue(field.Key, out string? value) || string.IsNullOrWhiteSpace(value)));
    }
}
