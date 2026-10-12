using DevCraft.Cli;

namespace DevCraft.Cli.Tests;

public sealed class FakeFeatureDirectoryOpener : IFeatureDirectoryOpener
{
    public bool Result { get; set; } = true;

    public List<string> OpenedDirectories { get; } = [];

    public bool Open(string directory)
    {
        OpenedDirectories.Add(directory);

        return Result;
    }
}
