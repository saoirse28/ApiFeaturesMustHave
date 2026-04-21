using Asp.Versioning;
namespace APIVersioning.Versioning
{
    /// <summary>
    /// Adds standard HTTP deprecation headers to responses for deprecated API versions:
    ///
    ///   Deprecation: true
    ///   Sunset: Tue, 01 Jul 2025 00:00:00 GMT    ← when the version is removed
    ///   Link: &lt;https://api.myapp.com/v3/orders&gt;; rel="successor-version"
    ///
    /// These headers allow API clients and monitoring tools to detect
    /// deprecated endpoints automatically without reading documentation.
    /// See RFC 8594 (Sunset) and RFC Deprecation HTTP Header draft.
    /// </summary>
    public sealed class DeprecationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DeprecationMiddleware> _logger;

        public DeprecationMiddleware(
            RequestDelegate next,
            ILogger<DeprecationMiddleware> logger)
        {
            _next   = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            // Read the resolved API version from the request after routing
            var apiVersionFeature = context.Features.Get<IApiVersioningFeature>();
            if (apiVersionFeature?.RequestedApiVersion is null)
                return;

            var version = apiVersionFeature.RequestedApiVersion.ToString();

            if (!ApiVersions.Deprecated.Contains(version))
                return;

            // RFC 8594 Sunset header
            if (ApiVersions.SunsetDates.TryGetValue(version, out var sunsetDate))
            {
                context.Response.Headers["Sunset"] =
                    sunsetDate.ToString("R");  // RFC 1123 format
            }

            // Draft deprecation header
            context.Response.Headers["Deprecation"] = "true";

            // Link to the successor version
            context.Response.Headers["Link"] =
                $"<https://api.myapp.com/v{ApiVersions.Latest}>; rel=\"successor-version\"";

            _logger.LogWarning(
                "Deprecated API version {Version} used on {Method} {Path} — " +
                "sunset: {Sunset}",
                version,
                context.Request.Method,
                context.Request.Path,
                ApiVersions.SunsetDates.GetValueOrDefault(version).ToString("yyyy-MM-dd"));
        }
    }
}