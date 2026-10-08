namespace DevCraft.Cli;

public interface IAiProjectScanner
{
    ProjectScanResult Scan(string directoryPath, string defaultAgent);
}

