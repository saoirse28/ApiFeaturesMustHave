using Microsoft.Extensions.Caching.Distributed;

namespace Caching.Caching;

/// <summary>
/// Resilient distributed cache wrapper — if Redis is unavailable,
/// operations silently fall through instead of throwing exceptions.
/// The application degrades to always hitting the database but stays alive.
///
/// Wrap IDistributedCache at registration time:
///   services.Decorate<IDistributedCache, ResilientDistributedCache>();
///   (requires Scrutor: dotnet add package Scrutor)
/// </summary>
public sealed class ResilientDistributedCache : IDistributedCache
{
    private readonly IDistributedCache _inner;
    private readonly ILogger<ResilientDistributedCache> _logger;

    public ResilientDistributedCache(
        IDistributedCache inner,
        ILogger<ResilientDistributedCache> logger)
    {
        _inner  = inner;
        _logger = logger;
    }

    public byte[]? Get(string key)
    {
        try { return _inner.Get(key); }
        catch (Exception ex) { Log(ex, key, "Get"); return null; }
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken ct = default)
    {
        try { return await _inner.GetAsync(key, ct); }
        catch (Exception ex) { Log(ex, key, "GetAsync"); return null; }
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        try { _inner.Set(key, value, options); }
        catch (Exception ex) { Log(ex, key, "Set"); }
    }

    public async Task SetAsync(string key, byte[] value,
        DistributedCacheEntryOptions options, CancellationToken ct = default)
    {
        try { await _inner.SetAsync(key, value, options, ct); }
        catch (Exception ex) { Log(ex, key, "SetAsync"); }
    }

    public void Remove(string key)
    {
        try { _inner.Remove(key); }
        catch (Exception ex) { Log(ex, key, "Remove"); }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try { await _inner.RemoveAsync(key, ct); }
        catch (Exception ex) { Log(ex, key, "RemoveAsync"); }
    }

    public void Refresh(string key)
    {
        try { _inner.Refresh(key); }
        catch (Exception ex) { Log(ex, key, "Refresh"); }
    }

    public async Task RefreshAsync(string key, CancellationToken ct = default)
    {
        try { await _inner.RefreshAsync(key, ct); }
        catch (Exception ex) { Log(ex, key, "RefreshAsync"); }
    }

    private void Log(Exception ex, string key, string operation) =>
        _logger.LogWarning(ex,
            "Redis cache {Operation} failed for key '{Key}' — degrading gracefully",
            operation, key);
}