using System.Net;
using System.Text.Json;

namespace TrainingTracker.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            string traceId = context.Items[CorrelationIdMiddleware.HeaderName]?.ToString() ?? Guid.NewGuid().ToString("n");
            _logger.LogError(ex, "Unhandled exception (traceId={TraceId})", traceId);
            await WriteErrorAsync(context, traceId, ex);
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, string traceId, Exception ex)
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";
        var payload = new
        {
            traceId,
            message = "An unexpected error occurred.",
            details = context.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true ? ex.Message : null
        };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}

public static class ExceptionHandlingExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<TrainingTracker.Api.Middleware.ExceptionHandlingMiddleware>();
}