using ServerSentEvents.SSE;
using System.Threading.Channels;

namespace ServerSentEvents.Channels;

/// <summary>
/// Per-connection bounded channel.
/// Drops the OLDEST event when full so slow clients
/// never block publishers or exhaust memory.
/// </summary>
public sealed class BoundedEventChannel : IAsyncDisposable
{
    private readonly Channel<SseEvent> _channel;
    private readonly ILogger<BoundedEventChannel> _logger;

    public string ConnectionId { get; }
    public string UserId { get; }
    public bool IsCompleted => _channel.Reader.Completion.IsCompleted;

    public BoundedEventChannel(
        string connectionId,
        string userId,
        ILogger<BoundedEventChannel> logger,
        int capacity = 100)
    {
        ConnectionId = connectionId;
        UserId       = userId;
        _logger      = logger;

        _channel = Channel.CreateBounded<SseEvent>(
            new BoundedChannelOptions(capacity)
            {
                FullMode             = BoundedChannelFullMode.DropOldest,
                SingleReader         = true,
                SingleWriter         = false,
                AllowSynchronousContinuations = false
            });
    }

    public bool TryWrite(SseEvent evt)
    {
        if (_channel.Writer.TryWrite(evt))
            return true;

        _logger.LogWarning(
            "Channel full — dropped {EventType} for connection {ConnectionId}",
            evt.EventType, ConnectionId);
        return false;
    }

    public IAsyncEnumerable<SseEvent> ReadAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);

    public void Complete() => _channel.Writer.TryComplete();

    public ValueTask DisposeAsync()
    {
        Complete();
        return ValueTask.CompletedTask;
    }
}