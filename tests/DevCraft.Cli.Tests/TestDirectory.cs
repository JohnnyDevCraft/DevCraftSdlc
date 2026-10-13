namespace DevCraft.Cli.Tests;

public sealed class TestDirectory : IDisposable
{
    public TestDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"devcraft-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        if (!Directory.Exists(Path))
        {
            return;
        }

        // Git marks object files read-only, which blocks deletion on Windows.
        foreach (string file in Directory.EnumerateFiles(Path, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(Path, recursive: true);
    }
}

