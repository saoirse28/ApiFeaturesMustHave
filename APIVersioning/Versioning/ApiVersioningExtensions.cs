using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace APIVersioning.Versioning
{
    public static class ApiVersioningExtensions
    {
        /// <summary>
        /// Registers API versioning with URL segment, header, and query string readers.
        /// Generates one Swagger document per supported API version.
        /// </summary>
        public static IServiceCollection AddApiVersionings(
            this IServiceCollection services)
        {
            services
                .AddApiVersioning(opts =>
                {
                    // Assume the latest version when no version is specified
                    // in the request — prevents breaking existing clients that
                    // pre-date versioning.
                    opts.DefaultApiVersion                  = new ApiVersion(3, 0);
                    opts.AssumeDefaultVersionWhenUnspecified = true;

                    // Adds api-supported-versions and api-deprecated-versions
                    // response headers — clients can discover available versions.
                    opts.ReportApiVersions = true;

                    // Accept version from multiple sources (in priority order):
                    //   1. URL segment:       /api/v2/orders
                    //   2. Header:            X-Api-Version: 2.0
                    //   3. Query string:      /api/orders?api-version=2.0
                    opts.ApiVersionReader = ApiVersionReader.Combine(
                        new UrlSegmentApiVersionReader(),
                        new HeaderApiVersionReader("X-Api-Version"),
                        new QueryStringApiVersionReader("api-version")
                    );
                })
                // Generates a separate IApiVersionDescriptionProvider
                // used by Swagger to create one doc per version.
                .AddApiExplorer(opts =>
                {
                    // "v{major}" → v1, v2, v3
                    opts.GroupNameFormat           = "'v'VVV";
                    opts.SubstituteApiVersionInUrl = true;
                });

            // Register Swagger options that create one SwaggerDoc per API version
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>,
                ConfigureSwaggerOptions>();

            services.AddSwaggerGen(opts =>
            {
                // Include XML doc comments in Swagger UI
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                    opts.IncludeXmlComments(xmlPath);

                // Resolve conflicts when multiple versions have the same route shape
                opts.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
            });

            return services;
        }

        /// <summary>
        /// Maps Swagger UI endpoints — one per API version.
        /// </summary>
        public static WebApplication UseApiVersioningSwagger(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(opts =>
            {
                var provider = app.Services
                    .GetRequiredService<IApiVersionDescriptionProvider>();

                // Create a Swagger UI dropdown entry for each version,
                // newest first so it opens on the current version by default.
                foreach (var description in provider.ApiVersionDescriptions
                             .OrderByDescending(d => d.ApiVersion))
                {
                    var name = description.IsDeprecated
                        ? $"{description.GroupName} [DEPRECATED]"
                        : description.GroupName;

                    opts.SwaggerEndpoint(
                        $"/swagger/{description.GroupName}/swagger.json",
                        name);
                }

                opts.RoutePrefix = "swagger";
            });

            return app;
        }
    }
}
