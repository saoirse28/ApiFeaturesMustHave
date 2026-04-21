namespace ExceptionHandling.Exceptions
{
    /// <summary>
    /// Thrown when input validation fails.
    /// Maps to HTTP 422 Unprocessable Entity.
    /// Carries a structured list of field-level errors.
    /// </summary>
    public sealed class ValidationException : DomainException
    {
        public IReadOnlyList<ValidationError> Errors { get; }

        public ValidationException(IEnumerable<ValidationError> errors)
            : base(
                message: "One or more validation errors occurred.",
                errorCode: "VALIDATION_FAILED",
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Validation Failed")
        {
            Errors = errors.ToList().AsReadOnly();
        }

        public ValidationException(string field, string message)
            : this(new[] { new ValidationError(field, message) })
        {
        }
    }

    public sealed record ValidationError(
        string Field,
        string Message,
        string? Code = null);
}
