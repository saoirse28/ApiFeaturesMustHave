using ServerSentEvents.SSE;

namespace ServerSentEvents.Channels
{
    public interface IEventChannel
    {
        public bool TryWrite(SseEvent evt);
        public IAsyncEnumerable<SseEvent> ReadAllAsync(CancellationToken ct);
        public void Complete(Exception? error = null);
        public ValueTask DisposeAsync();
    }
}
