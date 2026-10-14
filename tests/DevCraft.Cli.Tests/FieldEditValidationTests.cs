using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class FieldEditValidationTests
{
    [Fact]
    public void FieldEditValidationReturnsMissingRequiredField()
    {
        FieldEditField[] fields =
        [
            new("name", "Project name", Required: true),
            new("other", "Other"),
        ];
        Dictionary<string, string> values = new() { ["name"] = "   ", ["other"] = "value" };

        FieldEditField? missing = FieldEditValidation.FirstMissingRequired(fields, values);

        Assert.Equal("name", missing?.Key);
    }

    [Fact]
    public void FieldEditValidationAllowsBlankOptionalField()
    {
        FieldEditField[] fields = [new("other", "Other")];
        Dictionary<string, string> values = new() { ["other"] = string.Empty };

        Assert.Null(FieldEditValidation.FirstMissingRequired(fields, values));
    }

    [Fact]
    public void FieldEditValidationReturnsNullWhenRequiredFieldsFilled()
    {
        FieldEditField[] fields = [new("name", "Project name", Required: true)];
        Dictionary<string, string> values = new() { ["name"] = "DevCraft" };

        Assert.Null(FieldEditValidation.FirstMissingRequired(fields, values));
    }
}
