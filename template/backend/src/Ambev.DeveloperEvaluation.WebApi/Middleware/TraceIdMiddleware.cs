using Serilog.Context;
using System.Diagnostics;

namespace Ambev.DeveloperEvaluation.WebApi.Middleware;

public sealed class TraceIdMiddleware(
    RequestDelegate next,
    ILogger<TraceIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var activity = Activity.Current;
        var traceId = activity?.TraceId.ToString() ?? context.TraceIdentifier;
        var spanId = activity?.SpanId.ToString();
        var idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();

        context.Response.Headers["X-Trace-Id"] = traceId;

        // Use Serilog LogContext so TraceId/SpanId become structured log attributes.
        // The OpenTelemetry Collector promotes trace_id to a Loki index label.
        using var traceScope = LogContext.PushProperty("TraceId", traceId);
        using var spanScope = string.IsNullOrWhiteSpace(spanId)
            ? null
            : LogContext.PushProperty("SpanId", spanId);
        using var requestScope = LogContext.PushProperty("RequestId", context.TraceIdentifier);
        using var idempotencyScope = string.IsNullOrWhiteSpace(idempotencyKey)
            ? null
            : LogContext.PushProperty("IdempotencyKey", idempotencyKey);

        logger
            .LogDebug(
                "HTTP request started. Method={Method}, Path={Path}, TraceId={TraceId}",
                context.Request.Method,
                context.Request.Path,
                traceId);

        await next(context);
    }
}
