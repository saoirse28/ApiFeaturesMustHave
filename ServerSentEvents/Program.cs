using ServerSentEvents.BackgroundServices;
using ServerSentEvents.Channels;
using ServerSentEvents.MinimalApis;
using ServerSentEvents.Publishers;
using ServerSentEvents.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization(opts =>
    opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin")));

// SSE core
builder.Services.AddSingleton<EventChannelRegistry>();
builder.Services.AddSingleton<IEventPublisher, EventPublisher>();
builder.Services.AddHostedService<SseHeartbeatService>();

// Domain services
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// OpenAPI
builder.Services.AddOpenApi();

// ── Kestrel — long-lived SSE connections ──────────────────────────────────────
builder.WebHost.ConfigureKestrel(opts =>
{
    opts.Limits.KeepAliveTimeout      = TimeSpan.FromHours(1);
    opts.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);
});

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseHttpsRedirection();
app.MapOpenApi();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── SSE endpoints ─────────────────────────────────────────────────────────────
app.MapSseEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

}
app.Run();