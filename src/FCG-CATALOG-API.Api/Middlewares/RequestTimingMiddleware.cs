using System.Diagnostics;

namespace FCG_CATALOG_API.Api;

public class RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
         
        context.Response.OnStarting(() =>
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            context.Response.Headers["X-Response-Time-ms"] = elapsedMs.ToString();
            logger.LogInformation("Request took {ElapsedMs} ms", elapsedMs);

            return Task.CompletedTask;
        });

        await next(context);
    }
}