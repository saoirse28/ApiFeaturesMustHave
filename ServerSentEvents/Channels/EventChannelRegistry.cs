using ServerSentEvents.SSE;
using System.Collections.Concurrent;

namespace ServerSentEvents.Channels;

/// <summary>
/// Thread-safe registry: userId → active SSE channels.
/// One user can hold multiple connections (multiple tabs/devices).
/// Registered as Singleton.
/// </summary>
public sealed class EventChannelRegistry
{
    private readonly ConcurrentDictionary<
        string,
        ConcurrentDictionary<string, BoundedEventChannel>> _users = new();

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<EventChannelRegistry> _logger;

    public EventChannelRegistry(
        ILoggerFactory loggerFactory,
        ILogger<EventChannelRegistry> logger)
    {
        _loggerFactory = loggerFactory;
        _logger        = logger;
    }

    // ── Registration ──────────────────────────────────────────────────────────

    public BoundedEventChannel Register(
        string userId,
        string connectionId,
        int capacity = 100)
    {
        var channel = new BoundedEventChannel(
            connectionId, userId,
            _loggerFactory.CreateLogger<BoundedEventChannel>(),
            capacity);

        var userChannels = _users.GetOrAdd(
            userId,
            _ => new ConcurrentDictionary<string, BoundedEventChannel>());

        userChannels[connectionId] = channel;

        _logger.LogInformation(
            "SSE registered — user={UserId} conn={ConnectionId} total={Total}",
            userId, connectionId, userChannels.Count);

        return channel;
    }

    public void Unregister(string userId, string connectionId)
    {
        if (!_users.TryGetValue(userId, out var userChannels)) return;

        if (userChannels.TryRemove(connectionId, out var channel))
        {
            channel.Complete();
            _logger.LogInformation(
                "SSE unregistered — user={UserId} conn={ConnectionId}",
                userId, connectionId);
        }

        if (userChannels.IsEmpty)
            _users.TryRemove(userId, out _);
    }

    // ── Publishing ────────────────────────────────────────────────────────────

    public int PublishToUser(string userId, SseEvent evt)
    {
        if (!_users.TryGetValue(userId, out var channels)) return 0;

        var delivered = 0;
        foreach (var (id, ch) in channels)
        {
            if (ch.IsCompleted) { channels.TryRemove(id, out _); continue; }
            if (ch.TryWrite(evt)) delivered++;
        }
        return delivered;
    }

    public int BroadcastToAll(SseEvent evt)
    {
        var delivered = 0;
        foreach (var (_, channels) in _users)
            foreach (var (_, ch) in channels)
                if (ch.TryWrite(evt)) delivered++;
        return delivered;
    }

    public int PublishToGroup(IEnumerable<string> userIds, SseEvent evt)
        => userIds.Sum(uid => PublishToUser(uid, evt));

    // ── Stats ─────────────────────────────────────────────────────────────────

    public int TotalConnections
        => _users.Values.Sum(c => c.Count);

    public int ConnectedUsers
        => _users.Count(kv => !kv.Value.IsEmpty);
}