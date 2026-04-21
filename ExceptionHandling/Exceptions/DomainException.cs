namespace ExceptionHandling.Exceptions
{
    /// <summary>
    /// Base class for all domain-level exceptions.
    /// Every custom exception in the app inherits from this.
    /// </summary>
    public abstract class DomainException : Exception
    {
        public string ErrorCode { get; }
        public int StatusCode { get; }
        public string Title { get; }
        public IReadOnlyDictionary<string, object?> Extensions { get; }

        protected DomainException(
            string message,
            string errorCode,
            int statusCode,
            string title,
            Dictionary<string, object?>? extensions = null)
            : base(message)
        {
            ErrorCode  = errorCode;
            StatusCode = statusCode;
            Title      = title;
            Extensions = extensions ?? new Dictionary<string, object?>();
        }
    }
}
