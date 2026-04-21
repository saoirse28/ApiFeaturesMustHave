namespace Resilience.Infrastructure
{
    public interface IDbConnection
    {
        public Task<T?> QueryFirstOrDefaultAsync<T>(
            string sql,
            object? param = null,
            CancellationToken ct = default);
        public Task ExecuteAsync(
            string sql,
            object? param = null,
            CancellationToken ct = default);
        public Task<List<T>> QueryAsync<T>(
            string sql,
            object? param = null,
            CancellationToken ct = default);
    }
}
