namespace ExceptionHandling.Middleware
{
    /// <summary>
    /// Ensures every request has a correlation ID:
    ///   1. Reads X-Correlation-Id from the incoming request header.
    ///   2. If absent, generates a new one (UUID).
    ///   3. Stores it in HttpContext.Items for use anywhere in the pipeline.
    ///   4. Echoes it back in the response header so callers can trace their request.
    /// </summary>
    public sealed class CorrelationIdMiddleware
    {
        private const string HeaderName = "X-Correlation-Id";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                ?? Guid.NewGuid().ToString("N"); // compact UUID, no dashes

            // Store so exception handlers and services can read it
            context.Items["CorrelationId"] = correlationId;
            context.TraceIdentifier = correlationId;

            // Echo it back — clients can match their request to the server log
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.TryAdd(HeaderName, correlationId);
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}
