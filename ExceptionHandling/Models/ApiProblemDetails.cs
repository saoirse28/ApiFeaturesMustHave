using Microsoft.AspNetCore.Mvc;

namespace ExceptionHandling.Models
{
    /// <summary>
    /// Extends RFC 7807 ProblemDetails with app-specific fields:
    ///   - errorCode    — machine-readable error identifier for client logic
    ///   - correlationId — links to a specific log trace
    ///   - errors        — field-level validation errors (validation only)
    /// </summary>
    public sealed class ApiProblemDetails : ProblemDetails
    {
        /// <summary>Machine-readable error code. Clients can switch on this.</summary>
        public string? ErrorCode { get; init; }

        /// <summary>Correlation ID from the request — links to the server log entry.</summary>
        public string? CorrelationId { get; init; }

        /// <summary>Field-level validation errors (only populated on 422 responses).</summary>
        public IReadOnlyList<ValidationErrorDetail>? Errors { get; init; }

        /// <summary>UTC timestamp of the error occurrence.</summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public sealed record ValidationErrorDetail(
        string Field,
        string Message,
        string? Code = null);
}
