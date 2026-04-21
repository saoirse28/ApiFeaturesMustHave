namespace Resilience.Infrastructure
{
    public class DbConnection : IDbConnection
    {
        public Task ExecuteAsync(string sql, object? param = null, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task<List<T>> QueryAsync<T>(string? sql, object? param = null, CancellationToken ct = default)
        {
            return QueryAsync<T>(default);
        }

        public Task<T?> QueryFirstOrDefaultAsync<T>(string? sql, object? param = null, CancellationToken ct = default)
        {
            return Task.FromResult<T?>(default);
        }
    }
}
