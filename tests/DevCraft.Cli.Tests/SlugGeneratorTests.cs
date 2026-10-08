using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class SlugGeneratorTests
{
    [Theory]
    [InlineData("Operator Questions", "operator-questions")]
    [InlineData("C# Standards", "c-standards")]
    [InlineData("SwiftUI App", "swiftui-app")]
    [InlineData("ASP.Net Core", "asp-net-core")]
    public void CreateReturnsLowercaseKebabCase(string value, string expected)
    {
        Assert.Equal(expected, SlugGenerator.Create(value));
    }
}
