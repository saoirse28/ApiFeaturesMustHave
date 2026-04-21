using ServerSentEvents.Channels;
using ServerSentEvents.SSE;

namespace ServerSentEvents.Publishers;

/// <summary>
/// In-process publisher. For multi-instance, replace with RedisEventPublisher.
/// </summary>
public sealed class EventPublisher : IEventPublisher
{
    private readonly EventChannelRegistry _registry;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(
        EventChannelRegistry registry,
        ILogger<EventPublisher> logger)
    {
        _registry = registry;
        _logger   = logger;
    }

    public Task PublishToUserAsync<T>(
        string userId,
        string eventType,
        T payload,
        CancellationToken ct = default)
    {
        var evt = SseEvent.Create(eventType, payload);
        var delivered = _registry.PublishToUser(userId, evt);

        _logger.LogDebug(
            "Published {EventType} to user {UserId} → {Delivered} connection(s)",
            eventType, userId, delivered);

        return Task.CompletedTask;
    }

    public Task BroadcastAsync<T>(
        string eventType,
        T payload,
        CancellationToken ct = default)
    {
        var evt = SseEvent.Create(eventType, payload);
        var delivered = _registry.BroadcastToAll(evt);

        _logger.LogInformation(
            "Broadcast {EventType} → {Delivered} connection(s)",
            eventType, delivered);

        return Task.CompletedTask;
    }

    public Task PublishToGroupAsync<T>(
        IEnumerable<string> userIds,
        string eventType,
        T payload,
        CancellationToken ct = default)
    {
        var evt = SseEvent.Create(eventType, payload);
        var delivered = _registry.PublishToGroup(userIds, evt);

        _logger.LogDebug(
            "Group publish {EventType} → {Delivered} connection(s)",
            eventType, delivered);

        return Task.CompletedTask;
    }

    public int TotalConnections => _registry.TotalConnections;
    public int ConnectedUsers => _registry.ConnectedUsers;
}