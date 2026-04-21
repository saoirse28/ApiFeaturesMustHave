namespace ExceptionHandling.Exceptions
{
    /// <summary>
    /// Thrown when an operation conflicts with the current state of a resource.
    /// Maps to HTTP 409.
    /// </summary>
    public sealed class ConflictException : DomainException
    {
        public ConflictException(string resourceName, string reason)
            : base(
                message: $"Conflict on {resourceName}: {reason}",
                errorCode: "RESOURCE_CONFLICT",
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                extensions: new Dictionary<string, object?>
                {
                    ["resourceName"] = resourceName,
                    ["reason"]       = reason
                })
        {
        }
    }
}
