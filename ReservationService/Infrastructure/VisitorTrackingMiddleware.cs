using Prometheus;
using StackExchange.Redis;

namespace ReservationService.Infrastructure;

public class VisitorTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDatabase _redis;

    private static readonly Counter UniqueVisitors = Metrics
        .CreateCounter("unique_visitors_total", "Total unique visitors");

    public VisitorTrackingMiddleware(RequestDelegate next, IConnectionMultiplexer redis)
    {
        _next = next;
        _redis = redis.GetDatabase();
    }

    public async Task Invoke(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var browser = ExtractBrowserName(userAgent);

        var key = $"visitor:{date}:{ip}:{browser}";

        var exists = await _redis.KeyExistsAsync(key);

        if (!exists)
        {
            await _redis.StringSetAsync(key, "1");
            UniqueVisitors.Inc();
        }

        await _next(context);
    }

    private static string ExtractBrowserName(string userAgent)
    {
        if (userAgent.Contains("Chrome")) return "Chrome";
        if (userAgent.Contains("Firefox")) return "Firefox";
        if (userAgent.Contains("Edg")) return "Edge";
        return userAgent.Contains("Safari") ? "Safari" : "Other";
    }
}