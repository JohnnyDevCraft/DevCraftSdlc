namespace DevCraft.Cli;

public sealed record ProjectScanResult(
    ProjectProfile ProjectProfile,
    IReadOnlyList<DetectedProject> Projects,
    AiSdlcDetection AiDrivenSdlc);
