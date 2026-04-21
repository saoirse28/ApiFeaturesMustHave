using FeatureManagement.FeatureFlags;
using FeatureManagement.Middleware;
using FeatureManagement.Services;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

var builder = WebApplication.CreateBuilder(args);

// ── Feature Management ────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();          // required by custom filters
builder.Services.AddFeatureManagements();            // registers all filters + IFeatureManager

// Custom disabled-features handler (overrides default 404 behavior)
builder.Services.AddSingleton<IDisabledFeaturesHandler, FeatureNotEnabledHandler>();

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddScoped<LegacyCheckoutService>();
builder.Services.AddScoped<NewCheckoutService>();

// CheckoutServiceFactory is registered as ICheckoutService —
// consumers get feature-flag-aware routing transparently
builder.Services.AddScoped<ICheckoutService, CheckoutServiceFactory>();
builder.Services.AddScoped<IProductService, ProductService>();

// ── Other Services ────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Optional: Azure App Configuration (dynamic flag updates) ──────────────────
// Uncomment to enable live flag toggling from Azure without redeployment.
//
// builder.Configuration.AddAzureAppConfiguration(opts =>
// {
//     opts.Connect(builder.Configuration["AzureAppConfig:ConnectionString"])
//         .UseFeatureFlags(flagOpts =>
//         {
//             flagOpts.CacheExpirationInterval = TimeSpan.FromSeconds(30);
//         });
// });
// builder.Services.AddAzureAppConfiguration();

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────────
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Feature flag diagnostics — dev + staging only (middleware self-guards)
app.UseMiddleware<FeatureFlagDiagnosticsMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Optional: Azure App Configuration middleware for live refresh
// app.UseAzureAppConfiguration();

app.MapControllers();

app.Run();