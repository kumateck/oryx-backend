using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Globalization;
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

                if (violations.Count != 0)
                {
                    foreach (var date in violations)
                    {
                        logger.LogWarning("Holiday date violation detected: {Date}", date.ToShortDateString());
                    }

                    var problem = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Date Conflict with Holiday",
                        Detail = $"The following dates are holidays: {string.Join(", ", violations.Select(d => d.ToString("yyyy-MM-dd")))}"
                    };

                    context.Response.StatusCode = problem.Status.Value;
                    context.Response.ContentType = "application/problem+json";
                    await context.Response.WriteAsJsonAsync(problem);
                    return;
                }
            }
        }

        await next(context);
    }

    private static List<DateTime> ExtractDates(JToken token)
    {
        var result = new List<DateTime>();

        Traverse(token);
        return result;

        void Traverse(JToken current)
        {
            switch (current.Type)
            {
                case JTokenType.String when
                    DateTime.TryParse(current.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedDate):
                    result.Add(parsedDate.Date);
                    break;
                case JTokenType.Array:
                {
                    foreach (var item in current.Children())
                        Traverse(item);
                    break;
                }
                case JTokenType.Object:
                {
                    foreach (var prop in current.Children<JProperty>())
                        Traverse(prop.Value);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}