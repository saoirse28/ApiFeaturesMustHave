using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Resilience.HttpClients;
using Resilience.Infrastructure;
using Resilience.Resilience;
using Resilience.Services;
using ResilienceDemo.HttpClients;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ── Resilience pipelines ───────────────────────────────────────────────────────
builder.Services.AddStackExchangeRedisCache(opts =>
    opts.Configuration = builder.Configuration.GetConnectionString("Redis"));

// Register non-HTTP pipelines (DB, Redis, message bus)
builder.Services.AddResiliencePipelines(builder.Configuration);

// Register all typed HttpClients with their resilience handlers
builder.Services.AddHttpClients(builder.Configuration);

// ── Polly telemetry → OpenTelemetry ──────────────────────────────────────────

// Polly v8 emits these metrics automatically via System.Diagnostics.Metrics:
//   polly.resilience.pipeline.duration          — histogram per pipeline
//   polly.resilience.pipeline.executions        — counter per pipeline
//   polly.resilience.pipeline.open              — gauge: 1 = circuit open
//   polly.resilience.pipeline.retry             — retry attempt counter
//   polly.resilience.pipeline.hedging           — hedging attempt counter

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("MyApi", "1.0.0"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter("Polly")           // captures all Polly built-in metrics
        .AddMeter("MyApi.Metrics")
        .AddPrometheusExporter());

// ── Structured logging ────────────────────────────────────────────────────────
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")));

// ── Other services ────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ExternalCacheClient>();

builder.Services.AddScoped<IDbConnectionFactory, ConnectionFactory>();
builder.Services.AddScoped<IDbConnection,DbConnection>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPrometheusScrapingEndpoint("/metrics");
app.Run();