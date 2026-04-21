using ExceptionHandling.Extensions;
using ExceptionHandling.Repository;
using ExceptionHandling.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Exception Handling ───────────────────────────────────────────────────────
builder.Services.AddExceptionHandling();

// ── Other Services ───────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IOrderService,OrderService>();
builder.Services.AddSingleton<IOrderRepository,OrderRepository>();
builder.Services.AddScoped<ICurrentUserService,CurrentUserService>();

var app = builder.Build();

// ── Middleware Pipeline (ORDER IS CRITICAL) ──────────────────────────────────
// 1. Correlation ID — must be outermost so all logs can reference it
// 2. Exception handler — catches anything thrown below
// 3. Everything else
app.UseExceptionHandling();  // includes UseMiddleware<CorrelationIdMiddleware>()

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