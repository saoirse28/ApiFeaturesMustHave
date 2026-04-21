using Polly;
using Polly.Registry;
using Resilience.DTOs;
using Resilience.Resilience;

namespace Resilience.Infrastructure;

/// <summary>
/// Demonstrates using a non-HTTP resilience pipeline for database calls.
/// Injects ResiliencePipelineProvider<string> and retrieves the named pipeline.
/// The pipeline (retry + circuit breaker + timeout) is transparently applied
/// to every database operation.
/// </summary>
public sealed class OrderRepository : IOrderRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(
        IDbConnectionFactory db,
        ResiliencePipelineProvider<string> pipelineProvider,
        ILogger<OrderRepository> logger)
    {
        _db       = db;
        _pipeline = pipelineProvider.GetPipeline(ResiliencePipelineNames.Database);
        _logger   = logger;
    }

    public async Task<Order?> FindByIdAsync(string id, CancellationToken ct = default)
    {
        // Set a context key so the OnTimeout callback knows which operation timed out
        var context = ResilienceContextPool.Shared.Get(ct);
        context.Properties.Set(
            new ResiliencePropertyKey<string>("OperationKey"),
            $"FindOrder:{id}");

        try
        {
            return await _pipeline.ExecuteAsync(
                async token =>
                {
                    var connection = await _db.OpenAsync(ct);
                    return await connection.QueryFirstOrDefaultAsync<Order>(
                        "SELECT * FROM Orders WHERE Id = @Id",
                        new { Id = id });
                },
                context);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    public async Task SaveAsync(Order order, CancellationToken ct = default)
    {
        var context = ResilienceContextPool.Shared.Get(ct);
        context.Properties.Set(
            new ResiliencePropertyKey<string>("OperationKey"),
            $"SaveOrder:{order.Id}");

        try
        {
            await _pipeline.ExecuteAsync(
                async token =>
                {
                    var connection = await _db.OpenAsync(ct);
                    await connection.ExecuteAsync(
                        "INSERT INTO Orders (Id, CustomerId, Total, Status) " +
                        "VALUES (@Id, @CustomerId, @Total, @Status)",
                        order);
                },
                context);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    public async Task UpdateStatusAsync(
        string orderId,
        string status,
        CancellationToken ct = default) => await _pipeline.ExecuteAsync(
            async token =>
            {
                var connection = await _db.OpenAsync(ct);
                await connection.ExecuteAsync(
                    "UPDATE Orders SET Status = @Status, UpdatedAt = GETUTCDATE() " +
                    "WHERE Id = @Id",
                    new { Id = orderId, Status = status });
            },
            ct);

    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(
        string customerId,
        CancellationToken ct = default) => await _pipeline.ExecuteAsync(
            async token =>
            {
                var connection = await _db.OpenAsync(ct);
                var results = await connection.QueryAsync<Order>(
                    "SELECT * FROM Orders WHERE CustomerId = @CustomerId " +
                    "ORDER BY CreatedAt DESC",
                    new { CustomerId = customerId });
                return (IReadOnlyList<Order>)results.ToList();
            },
            ct);
}