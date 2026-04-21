using System.Net.ServerSentEvents;
using System.Text.Json;

namespace ServerSentEvents.SSE;

/// <summary>
/// Internal domain event envelope used inside the Channel pipeline.
/// Converted to SseItem&lt;T&gt; at the point of streaming.
/// </summary>
public sealed record SseEvent(
    string EventType,
    string Data,
    string? Id = null,
    int? RetryMs = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static SseEvent Create<T>(string eventType, T payload, string? id = null)
        => new(
            EventType: eventType,
            Data: JsonSerializer.Serialize(payload, JsonOptions),
            Id: id ?? Guid.NewGuid().ToString("N"));

    public static SseEvent Heartbeat()
        => new(
            EventType: SseEventTypes.Heartbeat,
            Data: JsonSerializer.Serialize(
                           new { timestamp = DateTimeOffset.UtcNow },
                           JsonOptions));

    public static SseEvent Connected(string connectionId)
        => new(
            EventType: SseEventTypes.Connected,
            Data: JsonSerializer.Serialize(
                           new { connectionId, timestamp = DateTimeOffset.UtcNow },
                           JsonOptions));

    /// <summary>
    /// Converts this envelope to the SseItem&lt;string&gt; that
    /// TypedResults.ServerSentEvents() consumes directly.
    /// </summary>
    public SseItem<string> ToSseItem()
        => new SseItem<string>(Data, EventType)
        {
            EventId = Id
        };
}