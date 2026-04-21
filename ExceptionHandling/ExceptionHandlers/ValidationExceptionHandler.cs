using ExceptionHandling.Models;
using Microsoft.AspNetCore.Diagnostics;


namespace ExceptionHandling.ExceptionHandlers
{
    /// <summary>
    /// Handles ValidationException separately so field-level errors are always
    /// included in the response body — not just a top-level detail string.
    /// Also handles FluentValidation.ValidationException if that package is used.
    /// </summary>
    public sealed class ValidationExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly ILogger<ValidationExceptionHandler> _logger;

        public ValidationExceptionHandler(
            IProblemDetailsService problemDetailsService,
            ILogger<ValidationExceptionHandler> logger)
        {
            _problemDetailsService = problemDetailsService;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // Handle our domain ValidationException
            if (exception is Exceptions.ValidationException validationEx)
            {
                return await HandleValidationAsync(
                    httpContext,
                    validationEx.Errors.Select(e => new ValidationErrorDetail(e.Field, e.Message, e.Code)),
                    cancellationToken);
            }

            // Also intercept FluentValidation's exception if used in the pipeline
            if (exception is FluentValidation.ValidationException fluentEx)
            {
                var errors = fluentEx.Errors.Select(e =>
                    new ValidationErrorDetail(e.PropertyName, e.ErrorMessage, e.ErrorCode));
                return await HandleValidationAsync(httpContext, errors, cancellationToken);
            }

            return false; // Not a validation exception — pass to next handler
        }

        private async Task<bool> HandleValidationAsync(
            HttpContext httpContext,
            IEnumerable<ValidationErrorDetail> errors,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Validation failed on {Method} {Path} with {ErrorCount} error(s)",
                httpContext.Request.Method,
                httpContext.Request.Path,
                errors.Count());

            httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

            var correlationId = httpContext.Request.Headers["X-Correlation-Id"]
                .FirstOrDefault() ?? httpContext.TraceIdentifier;

            var problemDetails = new ApiProblemDetails
            {
                Title         = "Validation Failed",
                Status        = StatusCodes.Status422UnprocessableEntity,
                Detail        = "One or more validation errors occurred. See 'errors' for details.",
                ErrorCode     = "VALIDATION_FAILED",
                CorrelationId = correlationId,
                Type          = "https://api.myapp.com/errors/validation-failed",
                Instance      = httpContext.Request.Path,
                Errors        = errors.ToList().AsReadOnly()
            };

            await _problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext    = httpContext,
                ProblemDetails = problemDetails
            });

            return true;
        }
    }
}
