namespace ServerSentEvents.Services
{
    public class DashboardService : IDashboardService
    {
        public Task<string> GetLiveMetricsAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetSnapshotAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
