using ProperLogging.Enrichers;
using ProperLogging.Logging;
using ProperLogging.Middleware;
using ProperLogging.Repository;
using ProperLogging.Services;
using Serilog;
using Serilog.Context;

// ── Bootstrap logger ──────────────────────────────────────────────────────────
// Captures startup errors BEFORE the host is built.
// Replaced by the full logger once configuration is loaded.

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting MyApi host");

    var builder = WebApplication.CreateBuilder(args);

    // ── Replace default logging with Serilog ──────────────────────────────────
    builder.Host.UseSerilog((ctx, services, config) => SerilogConfiguration
            .Build(ctx.Configuration, ctx.HostingEnvironment, config)
            // Inject ASP.NET Core services into enrichers via DI
            .ReadFrom.Services(services)
            );
        

    // ── Register enrichers (need IHttpContextAccessor) ────────────────────────
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSingleton<UserEnricher>();

    // ── Other services ────────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen();

    builder.Services.AddSingleton<IOrderRepository,OrderRepository>();
    builder.Services.AddScoped<IPaymentClient,PaymentClient>();
    builder.Services.AddScoped<IOrderService,OrderService>();

    var app = builder.Build();

    // ── Middleware pipeline (ORDER IS CRITICAL) ───────────────────────────────

    // 1. Correlation ID — must be first so every log has it from the start
    app.UseMiddleware<CorrelationIdMiddleware>();

    // 2. Serilog request logging — structured per-request log with timing
    //    enrichDiagnosticContext adds extra fields to the request completion log
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0}ms";

        opts.GetLevel = (ctx, elapsed, ex) =>
            ex is not null || ctx.Response.StatusCode >= 500
                ? Serilog.Events.LogEventLevel.Error
                : elapsed > 1000
                    ? Serilog.Events.LogEventLevel.Warning
                    : Serilog.Events.LogEventLevel.Information;

        opts.EnrichDiagnosticContext = (diag, ctx) =>
        {
            diag.Set(LoggingConstants.ClientIp,
                ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            diag.Set(LoggingConstants.UserAgent,
                ctx.Request.Headers.UserAgent.ToString());
            diag.Set(LoggingConstants.UserId,
                ctx.User?.FindFirst("sub")?.Value ?? "anonymous");
        };
    });

    // 3. Custom request logger (adds slow-request warnings beyond Serilog's default)
    app.UseMiddleware<RequestLoggingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    return 1;
}
finally
{
    // Always flush Serilog on shutdown — prevents lost log events
    await Log.CloseAndFlushAsync();
}

return 0;