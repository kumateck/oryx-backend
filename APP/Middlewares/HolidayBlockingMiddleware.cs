using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text;
using INFRASTRUCTURE.Context;

namespace APP.Middlewares;


public class HolidayBlockingMiddleware(RequestDelegate next, ILogger<HolidayBlockingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        if (context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put)
        {
            context.Request.EnableBuffering();

            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (!string.IsNullOrWhiteSpace(body) && body.TrimStart().StartsWith("{"))
            {
                var jToken = JToken.Parse(body);
                var allDates = ExtractDates(jToken);

                var holidayDates = dbContext.Holidays
                    .Where(h => h.DeletedAt == null)
                    .Select(h => h.Date.Date)
                    .ToHashSet();

                var violations = allDates.Where(date => holidayDates.Contains(date.Date)).ToList();

                if (violations.Count > 0)
                {
                    // Log and add a header with excluded dates
                    var excludedDates = violations.Select(d => d.ToString("yyyy-MM-dd")).ToList();

                    logger.LogInformation("Auto-removed holiday dates from request: {Dates}", string.Join(", ", excludedDates));

                    context.Response.OnStarting(() =>
                    {
                        context.Response.Headers["X-Excluded-Holiday-Dates"] = string.Join(", ", excludedDates);
                        return Task.CompletedTask;
                    });

                    // Remove dates from the JToken
                    RemoveDatesFromToken(jToken, holidayDates);

                    // Replace the request body with modified content
                    var newBody = Encoding.UTF8.GetBytes(jToken.ToString());
                    context.Request.Body = new MemoryStream(newBody);
                    context.Request.ContentLength = newBody.Length;
                }
            }
        }

        await next(context);
    }

    private static List<DateTime> ExtractDates(JToken token)
    {
        var result = new List<DateTime>();

        void Traverse(JToken current)
        {
            switch (current.Type)
            {
                case JTokenType.String:
                    var str = current.ToString();
                    if (str.Length >= 8 && DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedDate))
                    {
                        result.Add(parsedDate.Date);
                    }
                    break;

                case JTokenType.Array:
                    foreach (var item in current.Children()) Traverse(item);
                    break;

                case JTokenType.Object:
                    foreach (var prop in current.Children<JProperty>()) Traverse(prop.Value);
                    break;
            }
        }

        Traverse(token);
        return result;
    }

    private static void RemoveDatesFromToken(JToken token, HashSet<DateTime> holidayDates)
    {
        void Traverse(JToken current)
        {
            if (current.Type == JTokenType.Array)
            {
                var itemsToRemove = current.Children()
                    .Where(c => c.Type == JTokenType.String && DateTime.TryParse(c.ToString(), out var d) && holidayDates.Contains(d.Date))
                    .ToList();

                foreach (var item in itemsToRemove)
                {
                    item.Remove();
                }

                foreach (var item in current.Children())
                {
                    Traverse(item);
                }
            }
            else if (current.Type == JTokenType.Object)
            {
                foreach (var prop in current.Children<JProperty>())
                {
                    Traverse(prop.Value);
                }
            }
        }

        Traverse(token);
    }
}