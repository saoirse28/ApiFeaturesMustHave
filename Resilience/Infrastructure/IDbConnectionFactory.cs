namespace Resilience.Infrastructure
{
    public interface IDbConnectionFactory
    {
        public Task<IDbConnection> OpenAsync(CancellationToken ct = default);
    }
}
