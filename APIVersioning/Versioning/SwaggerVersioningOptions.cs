using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace APIVersioning.Versioning
{

    /// <summary>
    /// Generates one SwaggerDoc per API version at startup.
    /// Automatically picks up any new versions added to IApiVersionDescriptionProvider.
    /// </summary>
    public sealed class ConfigureSwaggerOptions
        : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;

        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
            => _provider = provider;

        public void Configure(SwaggerGenOptions opts)
        {
            foreach (var description in _provider.ApiVersionDescriptions)
            {
                opts.SwaggerDoc(
                    description.GroupName,
                    CreateInfoForApiVersion(description));
            }
        }

        private static OpenApiInfo CreateInfoForApiVersion(
            ApiVersionDescription description)
        {
            var info = new OpenApiInfo
            {
                Title   = "My API",
                Version = description.ApiVersion.ToString(),
                Contact = new OpenApiContact
                {
                    Name  = "API Team",
                    Email = "api-team@myapp.com",
                    Url   = new Uri("https://myapp.com/support")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT",
                    Url  = new Uri("https://opensource.org/licenses/MIT")
                }
            };

            if (description.IsDeprecated)
            {
                var sunsetDate = ApiVersions.SunsetDates
                    .TryGetValue(description.ApiVersion.ToString(), out var date)
                    ? date.ToString("yyyy-MM-dd")
                    : "TBD";

                info.Description =
                    $"**⚠️ DEPRECATED** — This API version is deprecated and will be " +
                    $"removed on **{sunsetDate}**. Please migrate to " +
                    $"[v{ApiVersions.Latest}](/swagger/v{ApiVersions.Latest}/swagger.json).";
            }
            else if (description.ApiVersion.ToString() == ApiVersions.Latest)
            {
                info.Description =
                    "**Current version** — recommended for all new integrations.";
            }

            return info;
        }
    }
}
