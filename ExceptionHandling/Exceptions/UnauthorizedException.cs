namespace ExceptionHandling.Exceptions
{
    /// <summary>
    /// Thrown when the caller is not authenticated.
    /// Maps to HTTP 401.
    /// </summary>
    public sealed class UnauthorizedException : DomainException
    {
        public UnauthorizedException(string? message = null)
            : base(
                message: message ?? "Authentication is required to access this resource.",
                errorCode: "UNAUTHORIZED",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized")
        {
        }
    }
}
