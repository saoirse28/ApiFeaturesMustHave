using Caching.Caching;
using Caching.Decorators;
using Caching.Repository;
using Caching.Services;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
var builder = WebApplication.CreateBuilder(args);

// ── Redis connection (singleton — shared by cache + invalidation) s─────────────
var configRedis = builder.Configuration["CacheConnection"];
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(configRedis));

// ── Caching layers ─────────────────────────────────────────────────────────────
builder.Services.AddCaching(builder.Configuration);

// ── Application services ───────────────────────────────────────────────────────

// Register the real implementation
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Register the decorator as IProductService — consumers get caching transparently
builder.Services.AddScoped<IProductService>(sp =>
    new CachedProductService(
        sp.GetRequiredService<ProductService>(),     // inner = real DB service
        sp.GetRequiredService<IDistributedCache>(),
        sp.GetRequiredService<ILogger<CachedProductService>>()));

// Memory cache service for in-process hot data
builder.Services.AddSingleton<MemoryCacheService>();

// Redis invalidation service
builder.Services.AddScoped<RedisInvalidationService>();

builder.Services.AddControllers();

var app = builder.Build();

// ── Middleware pipeline ────────────────────────────────────────────────────────
app.UseHttpsRedirection();
app.UseOutputCache();       // MUST be before MapControllers
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();