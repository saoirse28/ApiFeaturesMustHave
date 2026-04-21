using ServerSentEvents.Channels;
using ServerSentEvents.SSE;

namespace ServerSentEvents.BackgroundServices;

/// <summary>
/// Hosted service that sends a heartbeat event to all connected clients
/// every 30 seconds.
///
/// Why heartbeats are essential:
///   1. Proxies and load balancers (nginx, AWS ALB) close idle TCP connections
///      after their keep-alive timeout (typically 60s). A heartbeat every 30s
///      keeps the connection alive.
///   2. The browser's EventSource API detects a dropped connection only when
///      it tries to write. Heartbeats ensure clients detect disconnects quickly.
///   3. Prevents Kestrel from closing the response due to inactivity timeouts.
///
/// The heartbeat is written as an SSE comment line (": ping\n\n") which is
/// invisible to application-level event handlers but still flushes the
/// TCP buffer to keep the connection alive.
/// </summary>
public sealed class SseHeartbeatService(
    EventChannelRegistry registry,
    ILogger<SseHeartbeatService> logger) : BackgroundService
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(5);

    private readonly EventChannelRegistry _registry = registry;
    private readonly ILogger<SseHeartbeatService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
            "SSE heartbeat service started — interval: {Interval}s",
            HeartbeatInterval.TotalSeconds);

            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(HeartbeatInterval, ct);

                var heartbeat = SseEvent.Heartbeat();
                var connections = _registry.TotalConnections;

                if (connections > 0)
                {
                    _registry.BroadcastToAll(heartbeat);
                    _logger.LogDebug(
                        "Heartbeat sent to {Connections} SSE connection(s)",
                        connections);
                }
            }

        }
        catch (Exception ex)
        {
            _logger.LogInformation(
           ex.Message,
           HeartbeatInterval.TotalSeconds);
        }
        
    }
}