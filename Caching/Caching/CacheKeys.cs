namespace Caching.Caching;

/// <summary>
/// Single source of truth for all cache key patterns.
/// Centralizing prevents key typos and makes cache invalidation
/// discoverable — search by method name, not scattered string literals.
/// </summary>
public static class CacheKeys
{
    private const string Prefix = "myapi";

    // ── Products ─────────────────────────────────────────────────────────────
    public static string Product(string id)
        => $"{Prefix}:product:{id}";

    public static string ProductList(string? category = null, int page = 1, int pageSize = 20)
        => $"{Prefix}:products:list:{category ?? "all"}:p{page}:s{pageSize}";

    public static string ProductsByCategory(string category)
        => $"{Prefix}:products:cat:{category}";

    // ── Categories ────────────────────────────────────────────────────────────
    public static string Category(string id)
        => $"{Prefix}:category:{id}";

    public static string CategoryList()
        => $"{Prefix}:categories:all";

    // ── Users ─────────────────────────────────────────────────────────────────
    public static string UserProfile(string userId)
        => $"{Prefix}:user:{userId}:profile";

    public static string UserPermissions(string userId)
        => $"{Prefix}:user:{userId}:permissions";

    // ── Configuration ─────────────────────────────────────────────────────────
    public static string AppConfig(string key)
        => $"{Prefix}:config:{key}";

    // ── Prefix helpers for bulk invalidation ──────────────────────────────────
    public const string ProductsPrefix = $"{Prefix}:product";
    public const string CategoriesPrefix = $"{Prefix}:category";
    public const string UsersPrefix = $"{Prefix}:user";
}