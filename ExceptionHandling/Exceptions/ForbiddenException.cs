namespace ExceptionHandling.Exceptions
{
    /// <summary>
    /// Thrown when the caller is authenticated but lacks permission.
    /// Maps to HTTP 403.
    /// </summary>
    public sealed class ForbiddenException : DomainException
    {
        public ForbiddenException(string? resource = null)
            : base(
                message: resource is null
                                ? "You do not have permission to perform this action."
                                : $"You do not have permission to access '{resource}'.",
                errorCode: "FORBIDDEN",
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                extensions: resource is null
                                ? null
                                : new Dictionary<string, object?> { ["resource"] = resource })
        {
        }
    }
}
