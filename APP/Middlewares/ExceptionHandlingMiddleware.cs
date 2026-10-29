using System.Diagnostics;
using System.Text.Json;
using APP.Services.ErrorLogging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace APP.Middlewares;

/// <summary>
/// Gives every request a reference ID (X-Trace-Id header, and "traceId" in error bodies) that users
/// can quote, turns unhandled exceptions into a safe 500 response, and stores every error logged
/// during the request together with the request details.
/// </summary>
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    ErrorLogQueue errorLogQueue,
    IOptions<ErrorLogOptions> errorLogOptions)
{
    public const string TraceIdHeader = "X-Trace-Id";
    private const int ClientClosedRequest = 499;

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
        var request = new ErrorLogRequestContext(traceId);
        httpContext.Items[ErrorLogRequestContext.ItemKey] = request;
        httpContext.Response.OnStarting(() =>
        {
            httpContext.Response.Headers[TraceIdHeader] = traceId;
            return Task.CompletedTask;
        });
        // Lets the body be read again for the error entry after the endpoint consumed it.
        httpContext.Request.EnableBuffering();

        var stopwatch = Stopwatch.StartNew();
        var clientAborted = false;
        using var scope = logger.BeginScope(new Dictionary<string, object> { ["TraceId"] = traceId });
        try
        {
            await next(httpContext);
        }
        catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
        {
            // The caller went away (closed tab, timeout); nothing failed on our side.
            clientAborted = true;
            logger.LogInformation("Request {Method} {Path} was cancelled by the client.", httpContext.Request.Method, httpContext.Request.Path);
            if (!httpContext.Response.HasStarted) httpContext.Response.StatusCode = ClientClosedRequest;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception on {Method} {Path}: {Message}",
                httpContext.Request.Method, httpContext.Request.Path, exception.Message);

            if (!httpContext.Response.HasStarted)
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Server Error",
                    Detail = "An unexpected error occurred. Please try again later.",
                    Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
                    Extensions = { ["traceId"] = traceId },
                };

                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                httpContext.Response.ContentType = "application/problem+json";

                await httpContext.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                }));
            }
        }
        finally
        {
            stopwatch.Stop();
            await FlushAsync(httpContext, request, clientAborted, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task FlushAsync(HttpContext httpContext, ErrorLogRequestContext request, bool clientAborted, long elapsedMs)
    {
        try
        {
            var entries = request.Drain()
                .Where(e => !(clientAborted && e.Exception is OperationCanceledException))
                .Select(e => e.Entry)
                .ToList();

            // A 5xx produced without an exception (e.g. a failed Result mapped to 500) is still a server error.
            if (entries.Count == 0 && httpContext.Response.StatusCode >= 500 && !clientAborted)
            {
                var options = errorLogOptions.Value;
                entries.Add(ErrorLogFactory.Create(
                    "HTTP", LogLevel.Error, $"Request returned HTTP {httpContext.Response.StatusCode} without an exception.",
                    exception: null, messageTemplate: $"HTTP {httpContext.Response.StatusCode} {httpContext.Request.Path}",
                    options.ResolveInstance(), options.ResolveRelease()));
            }

            if (entries.Count == 0 || !errorLogQueue.Enabled) return;

            var body = await ErrorLogRequestSnapshot.ReadBodyAsync(httpContext.Request, errorLogOptions.Value.MaxRequestBodyChars);
            foreach (var entry in entries)
            {
                ErrorLogRequestSnapshot.Apply(entry, httpContext, request.TraceId, body, elapsedMs);
                errorLogQueue.Enqueue(entry);
            }
        }
        catch (Exception exception)
        {
            using var _ = ErrorLogSuppression.Begin();
            logger.LogWarning(exception, "Could not capture error details for {Path}.", httpContext.Request.Path);
        }
    }
}
