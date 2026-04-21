using ExceptionHandling.ExceptionHandlers;
using ExceptionHandling.Middleware;

namespace ExceptionHandling.Extensions
{
    public static class ExceptionHandlingExtensions
    {
        /// <summary>
        /// Registers all IExceptionHandler implementations in the correct order.
        /// Order matters — handlers run in registration sequence and stop at the first true return.
        /// </summary>
        public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
        {
            // RFC 7807 ProblemDetails service — required by all handlers above
            services.AddProblemDetails(opts =>
            {
                // Customize the default ProblemDetails for built-in middleware errors
                // (e.g. 404 from routing, 405 Method Not Allowed)
                opts.CustomizeProblemDetails = ctx =>
                {
                    ctx.ProblemDetails.Instance = ctx.HttpContext.Request.Path;
                    ctx.ProblemDetails.Extensions["correlationId"] =
                        ctx.HttpContext.Request.Headers["X-Correlation-Id"]
                            .FirstOrDefault() ?? ctx.HttpContext.TraceIdentifier;
                };
            });

            // Registration ORDER is the handling priority:
            // 1. Validation errors first (most specific — includes field errors)
            // 2. Domain exceptions second (all typed business exceptions)
            // 3. Global catch-all last (anything not handled above)
            services.AddExceptionHandler<ValidationExceptionHandler>();
            services.AddExceptionHandler<DomainExceptionHandler>();
            services.AddExceptionHandler<GlobalExceptionHandler>();

            return services;
        }

        public static WebApplication UseExceptionHandling(this WebApplication app)
        {
            // Activates the IExceptionHandler chain registered above
            app.UseExceptionHandler();

            // Generates ProblemDetails for status-code-only responses
            // (e.g. app.Use(...) that calls context.Response.StatusCode = 404 without a body)
            app.UseStatusCodePages();

            // Correlation ID middleware — MUST be first so it runs before any exception
            app.UseMiddleware<CorrelationIdMiddleware>();

            return app;
        }
    }
}
