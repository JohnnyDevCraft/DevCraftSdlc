namespace DevCraft.Cli;

public static class WorkItemUrl
{
    public const string InvalidMessage = "Work Item URL must be an absolute http or https URL.";

    public static bool IsValid(string value)
    {
        return Uri.TryCreate(value.Trim(), UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
