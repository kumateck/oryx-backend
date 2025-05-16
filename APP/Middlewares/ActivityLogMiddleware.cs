using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using APP.IRepository;
using DOMAIN.Entities.ActivityLogs;
using Microsoft.AspNetCore.Http;
using APP.Services.Background;
using SHARED.Services.Identity;

namespace APP.Middlewares;

public class ActivityLogMiddleware(RequestDelegate next)
{
    private readonly string[] _excludedPaths = ["/auth", "/collections", "/details", "/pagination", "/favicon.ico"];
    private readonly string[] _excludedMethods = ["OPTIONS"];
    private readonly string[] _allowedGetPaths = ["toggle-disable"];

    public async Task Invoke(HttpContext context, IActivityLogRepository repo, IBackgroundWorkerService backgroundService)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;

        var token = context.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();
        var userId = "";
        if (!string.IsNullOrEmpty(token))
        {
            var jwtToken = new JwtSecurityToken(token);
            userId = jwtToken.Subject ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? context.Connection.RemoteIpAddress?.ToString();
        var userAgent = request.Headers["User-Agent"].ToString();
        var queryParams = request.QueryString.ToString();
        var headers = JsonSerializer.Serialize(request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()));

        var pathValue = request.Path.Value?.ToLower();
        string requestBody = await ReadRequestBodyAsync(request);

        if (_excludedMethods.Contains(request.Method))
        {
            await next(context);
            return;
        }
        
        // Skip excluded paths (unless it's an allowed GET request)
        if (request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            if (_allowedGetPaths.Any(p => pathValue != null && pathValue.EndsWith(p)))
            {
                var model = GetModelFromPath(context.Request.Path);
                backgroundService.EnqueuePrevStateCapture(new PrevStateCaptureRequest
                { 
                    Method = request.Method,
                    Model = model?.ToLower(),
                    IpAddress = ipAddress,
                    UserId = userId,
                    RequestBody = requestBody
                });
            }
            await next(context);
            return;
        }
        
        if (_excludedPaths.Any(path => pathValue != null && pathValue.Contains(path)))
        {
            await next(context);
            return;
        }
        
        // Replace response body to capture it
        var originalBodyStream = context.Response.Body;
        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Rewind and read the response
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
            responseBodyStream.Seek(0, SeekOrigin.Begin);

            // Copy response to original stream
            await responseBodyStream.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream; // Restore original stream
            await responseBodyStream.DisposeAsync(); //dispose 

            var statusCode = context.Response.StatusCode;
            var executionTime = stopwatch.ElapsedMilliseconds;

            var action = GetAction(request.Path, request.Method);
            var modelPath = GetModelFromPath(context.Request.Path);

            var log = new CreateActivityLog
            {
                UserId = string.IsNullOrEmpty(userId) ? null : Guid.Parse(userId),
                Action = action,
                Module = ExtractModule(request.Path),
                SubModule = ExtractSubModule(request.Path),
                ActionType = DetermineActionType(request.Method),
                IpAddress = ipAddress,
                Url = $"{request.Scheme}://{request.Host}{request.Path}",
                HttpMethod = request.Method,
                StatusCode = statusCode,
                ExecutionTimeMs = executionTime,
                UserAgent = userAgent,
                QueryParams = queryParams,
                Headers = headers,
                Payload = requestBody,
                ResponsePayload = responseBody,
                CreatedAt = DateTime.UtcNow
            };

            backgroundService.EnqueueLog(log);

            // Optionally also enqueue prevState tracking if not a basic GET
            if (!request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                backgroundService.EnqueuePrevStateCapture(new PrevStateCaptureRequest
                { 
                    Method = request.Method,
                    Model = modelPath,
                    IpAddress = ipAddress,
                    UserId = userId,
                    RequestBody = requestBody
                });
            }
        }
    }

    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength == null || request.ContentLength == 0)
            return string.Empty;

        request.EnableBuffering();
        request.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Seek(0, SeekOrigin.Begin);
        return body;
    }

    private static string GetModelFromPath(PathString path)
    {
        const string versionPrefix = "v";
        var value = path.Value;
        var versionIndex = value?.IndexOf(versionPrefix, StringComparison.OrdinalIgnoreCase) ?? -1;
        if (versionIndex == -1) return null;

        versionIndex += versionPrefix.Length;
        var versionEndIndex = value.IndexOf("/", versionIndex, StringComparison.OrdinalIgnoreCase);
        var model = versionEndIndex >= 0 ? value[(versionEndIndex + 1)..] : value[versionIndex..];
        return model;
    }

    private static ActionType DetermineActionType(string method) =>
        method.ToUpperInvariant() switch
        {
            "POST" => ActionType.Create,
            "GET" => ActionType.Read,
            "PUT" or "PATCH" => ActionType.Update,
            "DELETE" => ActionType.Delete,
            _ => ActionType.Read
        };

    private static string ExtractModule(string path) => path.Split('/').Skip(1).FirstOrDefault() ?? "Unknown";

    private static string ExtractSubModule(string path) => path.Split('/').Skip(2).FirstOrDefault() ?? "Unknown";

    private static string GetAction(string path, string method) => $"{path} {method}";
}