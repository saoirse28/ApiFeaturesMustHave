using APIVersioning.MinimalApis;
using APIVersioning.Services;
using APIVersioning.Versioning;
var builder = WebApplication.CreateBuilder(args);

// ── API Versioning + Swagger ──────────────────────────────────────────────────
builder.Services.AddApiVersionings();          // registers versioning + swagger docs

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// ── MVC ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────────
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Deprecation headers (Sunset + Deprecation) on responses for deprecated versions
app.UseMiddleware<DeprecationMiddleware>();

// ── Swagger ───────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    app.UseApiVersioningSwagger();

// ── Controllers ───────────────────────────────────────────────────────────────
app.MapControllers();

// ── Minimal API versioned endpoints ───────────────────────────────────────────
app.MapProductEndpoints();

app.Run();