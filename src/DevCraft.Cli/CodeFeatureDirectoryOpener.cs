namespace DevCraft.Cli;

using System.ComponentModel;
using System.Diagnostics;

public sealed class CodeFeatureDirectoryOpener : IFeatureDirectoryOpener
{
    public bool Open(string directory)
    {
        if (Environment.GetEnvironmentVariable("DEVCRAFT_DISABLE_CLIENT_LAUNCH") == "1")
        {
            return true;
        }

        ProcessStartInfo startInfo = new()
        {
            FileName = OperatingSystem.IsWindows() ? "code.cmd" : "code",
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(directory);

        try
        {
            using Process? process = Process.Start(startInfo);

            return process is not null;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }
}
