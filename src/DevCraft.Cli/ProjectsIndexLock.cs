namespace DevCraft.Cli;

// Serializes read-modify-write of projects.json across DevCraft sessions. The lock is the open handle,
// so the operating system releases it if the process dies; a leftover lock file blocks nothing.
public static class ProjectsIndexLock
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(25);

    public static IDisposable Acquire(string profileDirectory, TimeSpan? timeout = null)
    {
        string path = SystemCentralProjectsStore.PathFor(profileDirectory) + ".lock";
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        DateTime deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);

        while (true)
        {
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (DateTime.UtcNow < deadline)
            {
                Thread.Sleep(RetryDelay);
            }
            catch (IOException)
            {
                throw new ProjectsIndexBusyException();
            }
        }
    }
}
