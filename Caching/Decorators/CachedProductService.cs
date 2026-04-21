using Caching.Caching;
using Caching.Models;
using Caching.Services;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Caching.Decorators;

/// <summary>
/// Decorator pattern — wraps IProductService with a transparent Redis caching layer.
/// Consumers inject IProductService and are completely unaware caching exists.
///
/// Cache-aside strategy:
///   1. Check Redis → return if hit.
///   2. Call inner service (DB) on miss.
///   3. Store result in Redis for future requests.
///   4. Invalidate relevant keys on mutations (Create, Update, Delete).
/// </summary>
public sealed class CachedProductService : IProductService
{
    private readonly IProductService _inner;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedProductService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public CachedProductService(
        IProductService inner,
        IDistributedCache cache,
        ILogger<CachedProductService> logger)
    {
        _inner  = inner;
        _cache  = cache;
        _logger = logger;
    }

    // ── READ: GetById ─────────────────────────────────────────────────────────

    public async Task<Product?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var key = CacheKeys.Product(id);

        var cached = await TryGetFromCacheAsync<Product>(key, ct);
        if (cached.Found) return cached.Value;

        var product = await _inner.GetByIdAsync(id, ct);

        if (product is not null)
            await SetCacheAsync(key, product, CacheEntryOptions.Standard, ct);

        return product;
    }

    // ── READ: GetList ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<Product>> GetListAsync(
        string? category, int page, int pageSize, CancellationToken ct = default)
    {
        var key = CacheKeys.ProductList(category, page, pageSize);

        var cached = await TryGetFromCacheAsync<List<Product>>(key, ct);
        if (cached.Found) return cached.Value!;

        var list = await _inner.GetListAsync(category, page, pageSize, ct);
        await SetCacheAsync(key, list, CacheEntryOptions.Standard, ct);
        return list;
    }

    // ── WRITE: Create ─────────────────────────────────────────────────────────

    public async Task<Product> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var product = await _inner.CreateAsync(request, ct);

        // Cache the new individual product immediately
        await SetCacheAsync(CacheKeys.Product(product.Id), product, CacheEntryOptions.Standard, ct);

        // Invalidate all list caches — they're now stale
        await InvalidateListCachesAsync(ct);

        return product;
    }

    // ── WRITE: Update ─────────────────────────────────────────────────────────

    public async Task<Product> UpdateAsync(
        string id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await _inner.UpdateAsync(id, request, ct);

        // Refresh the individual product cache with new data
        await SetCacheAsync(CacheKeys.Product(id), product, CacheEntryOptions.Standard, ct);

        // Invalidate list caches — sort order, filters may be affected
        await InvalidateListCachesAsync(ct);

        return product;
    }

    // ── WRITE: Delete ─────────────────────────────────────────────────────────

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _inner.DeleteAsync(id, ct);

        // Remove the deleted product from cache
        await _cache.RemoveAsync(CacheKeys.Product(id), ct);

        // Invalidate list caches
        await InvalidateListCachesAsync(ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(bool Found, T? Value)> TryGetFromCacheAsync<T>(
        string key, CancellationToken ct)
    {
        try
        {
            var bytes = await _cache.GetAsync(key, ct);
            if (bytes is null)
            {
                _logger.LogDebug("Cache MISS: {Key}", key);
                return (false, default);
            }

            var value = JsonSerializer.Deserialize<T>(bytes, JsonOptions);
            _logger.LogDebug("Cache HIT: {Key}", key);
            return (true, value);
        }
        catch (Exception ex)
        {
            // Redis unavailable — degrade gracefully, don't crash
            _logger.LogWarning(ex, "Cache read failed for key {Key} — falling back to source", key);
            return (false, default);
        }
    }

    private async Task SetCacheAsync<T>(
        string key, T value, DistributedCacheEntryOptions options, CancellationToken ct)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            await _cache.SetAsync(key, bytes, options, ct);
            _logger.LogDebug("Cache SET: {Key} (expires {Expiry})",
                key, options.AbsoluteExpirationRelativeToNow);
        }
        catch (Exception ex)
        {
            // Redis unavailable — write-through failure is non-fatal
            _logger.LogWarning(ex, "Cache write failed for key {Key}", key);
        }
    }

    private async Task InvalidateListCachesAsync(CancellationToken ct)
    {
        // Remove known list key patterns
        // For full prefix-scan invalidation, use RedisInvalidationService below
        var keysToRemove = new[]
        {
            CacheKeys.ProductList(),
            CacheKeys.ProductList(null, 1, 20),
            CacheKeys.ProductList(null, 1, 50),
        };

        foreach (var key in keysToRemove)
        {
            try { await _cache.RemoveAsync(key, ct); }
            catch { /* non-fatal */ }
        }
    }
}
