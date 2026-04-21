namespace ServerSentEvents.Services
{
    public interface IDashboardService
    {
        public Task<string> GetSnapshotAsync(CancellationToken ct);
        public Task<string> GetLiveMetricsAsync(CancellationToken ct);

    }
}
