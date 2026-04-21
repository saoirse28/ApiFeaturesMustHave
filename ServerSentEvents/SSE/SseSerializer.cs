using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServerSentEvents.SSE;

/// <summary>
/// Formats SseEvent objects into the text/event-stream wire format
/// defined in the HTML Living Standard.
///
/// Wire format:
///   id: {id}\n
///   event: {type}\n
///   data: {line1}\n
///   data: {line2}\n     ← multi-line data: one "data:" per line
///   retry: {ms}\n
///   \n                  ← blank line signals end of event
/// </summary>
public static class SseSerializer
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented               = false
    };

    /// <summary>
    /// Serializes an SseEvent to the text/event-stream wire format.
    /// </summary>
    public static string Serialize(SseEvent evt)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(evt.Id))
            sb.Append("id: ").AppendLine(evt.Id);

        if (!string.IsNullOrEmpty(evt.EventType) && evt.EventType != SseEventTypes.Message)
            sb.Append("event: ").AppendLine(evt.EventType);

        if (evt.RetryMs.HasValue)
            sb.Append("retry: ").AppendLine(evt.RetryMs.Value.ToString());

        // Multi-line data: split on newlines and prefix each line with "data: "
        if (!string.IsNullOrEmpty(evt.Data))
        {
            foreach (var line in evt.Data.Split('\n'))
                sb.Append("data: ").AppendLine(line);
        }
        else
        {
            // Empty data field — still required for the client to receive the event
            sb.AppendLine("data: ");
        }

        // Blank line — signals end of event to the browser
        sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// Writes an SSE event directly to the response stream.
    /// </summary>
    public static async Task WriteAsync(
        HttpResponse response,
        SseEvent evt,
        CancellationToken ct = default)
    {
        var text = Serialize(evt);
        await response.WriteAsync(text, ct);
        await response.Body.FlushAsync(ct);
    }

    /// <summary>
    /// Writes a comment line — used for keep-alive pings.
    /// Comment lines start with ":" and are ignored by EventSource.
    /// They prevent proxies and load balancers from closing idle connections.
    /// </summary>
    public static async Task WriteCommentAsync(
        HttpResponse response,
        string comment = "ping",
        CancellationToken ct = default)
    {
        await response.WriteAsync($": {comment}\n\n", ct);
        await response.Body.FlushAsync(ct);
    }
}