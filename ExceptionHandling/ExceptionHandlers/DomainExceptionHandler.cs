using ExceptionHandling.Exceptions;
using ExceptionHandling.Models;
using Microsoft.AspNetCore.Diagnostics;

namespace ExceptionHandling.ExceptionHandlers
{
    /// <summary>
    /// Handles all DomainException subtypes (Not Found, Conflict, Forbidden, Unauthorized).
    /// Produces RFC 7807-compliant ApiProblemDetails responses.
    /// Returns false for non-domain exceptions so the next handler runs.
    /// </summary>
    public sealed class DomainExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly ILogger<DomainExceptionHandler> _logger;

        public DomainExceptionHandler(
            IProblemDetailsService problemDetailsService,
            ILogger<DomainExceptionHandler> logger)
        {
            _problemDetailsService = problemDetailsService;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // Only handle domain exceptions — pass others down the chain
            if (exception is not DomainException domainException)
                return false;

            // Log at Warning level — domain exceptions are expected, not bugs
            _logger.LogWarning(
                "Domain exception {ErrorCode} on {Method} {Path}: {Message}",
                domainException.ErrorCode,
                httpContext.Request.Method,
                httpContext.Request.Path,
                domainException.Message);

            httpContext.Response.StatusCode = domainException.StatusCode;

            var correlationId = httpContext.Request.Headers["X-Correlation-Id"]
                .FirstOrDefault() ?? httpContext.TraceIdentifier;

            var problemDetails = new ApiProblemDetails
            {
                Title        = domainException.Title,
                Status       = domainException.StatusCode,
                Detail       = domainException.Message,
                ErrorCode    = domainException.ErrorCode,
                CorrelationId = correlationId,
                Type         = $"https://api.myapp.com/errors/{domainException.ErrorCode.ToLower().Replace('_', '-')}",
                Instance     = httpContext.Request.Path
            };

            // Copy domain-specific extension data into ProblemDetails.Extensions
            foreach (var (key, value) in domainException.Extensions)
                problemDetails.Extensions[key] = value;

            await _problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext    = httpContext,
                ProblemDetails = problemDetails,
                Exception      = exception
            });

            return true; // Handled — stop the chain
        }
    }
}
