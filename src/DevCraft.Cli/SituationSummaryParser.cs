using System.Text.Json;

namespace DevCraft.Cli;

public static class SituationSummaryParser
{
    public static string Parse(string value)
    {
        string payload = JsonPayloadExtractor.Extract(value);

        try
        {
            CompressionSummaryJson? result = JsonSerializer.Deserialize<CompressionSummaryJson>(
                payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!string.IsNullOrWhiteSpace(result?.SummaryData))
            {
                return result.SummaryData.Trim();
            }
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("AI compression did not return valid summary JSON.", exception);
        }

        throw new InvalidOperationException("AI compression returned an empty summary.");
    }
}
