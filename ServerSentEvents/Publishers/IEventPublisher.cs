namespace ServerSentEvents.Publishers;

public interface IEventPublisher
{
    Task PublishToUserAsync<T>(
        string userId,
        string eventType,
        T payload,
        CancellationToken ct = default);

    Task BroadcastAsync<T>(
        string eventType,
        T payload,
        CancellationToken ct = default);

    Task PublishToGroupAsync<T>(
        IEnumerable<string> userIds,
        string eventType,
        T payload,
        CancellationToken ct = default);

    int TotalConnections { get; }
    int ConnectedUsers { get; }
}