namespace Resilience.Infrastructure
{
    public class ConnectionFactory : IDbConnectionFactory
    {
        public Task<IDbConnection> OpenAsync(CancellationToken ct = default)
        {
            return Task.FromResult<IDbConnection>(new DbConnection());
        }
    }
}
