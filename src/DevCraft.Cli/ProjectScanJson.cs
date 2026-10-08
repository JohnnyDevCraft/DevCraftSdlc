namespace DevCraft.Cli;

public sealed class ProjectScanJson
{
    public ProjectProfileJson? ProjectProfile { get; set; }
    public List<ProjectJson> Projects { get; set; } = [];
    public AiDrivenSdlcJson? AiDrivenSdlc { get; set; }
}
