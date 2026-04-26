using MarketMind.Application.DTOs;
using System.Text.Json;

namespace MarketMind.API.Middleware;

// Catches any unhandled exception anywhere in the pipeline
// Returns consistent ApiResponse envelope instead of raw stack trace
public class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Unhandled exception on {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            context.Response.StatusCode  = 500;
            context.Response.ContentType = "application/json";

            var error = ApiResponse<object>.Fail(
                "An unexpected error occurred. Please try again.");

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(error));
        }
    }
}