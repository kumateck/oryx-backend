using APP.IRepository;
using Microsoft.AspNetCore.Http;

namespace APP.Middlewares;

public class ActivityLogMiddleware(RequestDelegate next, IActivityLogRepository repo)
{
    public async Task Invoke(HttpContext context)
    {
    }
}