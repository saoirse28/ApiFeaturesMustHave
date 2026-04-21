using ExceptionHandling.Models;
using Microsoft.AspNetCore.Diagnostics;

namespace ExceptionHandling.ExceptionHandlers
{
    /// <summary>
    /// Catch-all fallback handler for any unhandled exception.
    /// Always registered LAST in the DI container.
    /// Logs at Error level and returns a safe 500 response —
    /// NEVER exposes stack traces or internal details to the caller.
    /// </summary>
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionHandler(
            IProblemDetailsService problemDetailsService,
            ILogger<GlobalExceptionHandler> logger,
            IHostEnvironment environment)
        {
            _problemDetailsService = problemDetailsService;
            _logger = logger;
            _environment = environment;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var correlationId = httpContext.Request.Headers["X-Correlation-Id"]
                .FirstOrDefault() ?? httpContext.TraceIdentifier;

            // Log full exception details internally — including stack trace
            _logger.LogError(
                exception,
                "Unhandled exception [{CorrelationId}] on {Method} {Path}",
                correlationId,
                httpContext.Request.Method,
                httpContext.Request.Path);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            // In development: include exception detail to aid debugging
            // In production: return a safe, generic message only
            var detail = _environment.IsDevelopment()
                ? $"{exception.GetType().Name}: {exception.Message}"
                : "An unexpected error occurred. Please try again later.";

            var problemDetails = new ApiProblemDetails
            {
                Title         = "Internal Server Error",
                Status        = StatusCodes.Status500InternalServerError,
                Detail        = detail,
                ErrorCode     = "INTERNAL_SERVER_ERROR",
                CorrelationId = correlationId,
                Type          = "https://api.myapp.com/errors/internal-server-error",
                Instance      = httpContext.Request.Path
            };

            // In development only: include stack trace in extensions
            if (_environment.IsDevelopment())
            {
                problemDetails.Extensions["exceptionType"]  = exception.GetType().FullName;
                problemDetails.Extensions["stackTrace"]     = exception.StackTrace;
            }

            await _problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext    = httpContext,
                ProblemDetails = problemDetails,
                Exception      = exception
            });

            return true; // Always handled — swallow and respond 500
        }
    }
}
