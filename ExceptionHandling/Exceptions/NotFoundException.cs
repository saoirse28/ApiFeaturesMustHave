namespace ExceptionHandling.Exceptions
{
    /// <summary>
    /// Thrown when a requested resource does not exist.
    /// Maps to HTTP 404.
    /// </summary>
    public sealed class NotFoundException : DomainException
    {
        public NotFoundException(string resourceName, object resourceId)
            : base(
                message: $"{resourceName} with id '{resourceId}' was not found.",
                errorCode: "RESOURCE_NOT_FOUND",
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource Not Found",
                extensions: new Dictionary<string, object?>
                {
                    ["resourceName"] = resourceName,
                    ["resourceId"]   = resourceId?.ToString()
                })
        {
        }

        public NotFoundException(string message)
            : base(
                message: message,
                errorCode: "RESOURCE_NOT_FOUND",
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource Not Found")
        {
        }
    }
}
