using RateLimiting.Middleware;
using RateLimiting.RateLimiting;
using RateLimiting.Services;
using StackExchange.Redis;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiting(builder.Configuration);

// ── Redis (for distributed rate limiting) ────────────────────────────────────
builder.Services.AddStackExchangeRedisCache(opts =>
    opts.Configuration = builder.Configuration.GetConnectionString("Redis"));
builder.Services.AddSingleton<RedisRateLimiter>();

// ── Other services ────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductCatalogService, ProductCatalogService>();

var app = builder.Build();

// ── Middleware Pipeline (ORDER IS CRITICAL) ───────────────────────────────────
// Rate limiting must be placed AFTER routing so endpoint metadata is available,
// but BEFORE authentication so unauthenticated requests are still rate-limited.



app.UseHttpsRedirection();
app.UseRouting();

// Rate limiter — applies before auth so anonymous endpoints are also protected
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Adds X-RateLimit-* headers to responses
app.UseMiddleware<RateLimitHeadersMiddleware>();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();