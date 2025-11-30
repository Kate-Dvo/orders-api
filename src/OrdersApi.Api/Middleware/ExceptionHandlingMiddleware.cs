using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace OrdersApi.Api.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;

        logger.LogError(
            exception,
            "Unhandled exception occured. CorrelationId: {correlationId}, Path: {path}, Method {Method}",
            correlationId,
            context.Request.Path,
            context.Request.Method
        );

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "An error occured while processing your request.",
            Status = StatusCodes.Status500InternalServerError,
            Instance = context.Request.Path,
            Detail = environment.IsDevelopment()
                ? exception.ToString()
                : "An internal server error occurred. Please contact support with the correlation ID.",
            Extensions =
            {
                ["correlationId"] = correlationId,
                ["traceId"] = context.TraceIdentifier,
                ["timestamp"] = DateTime.UtcNow
            }
        };

        if (environment.IsDevelopment())
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = environment.IsDevelopment()
        });

        await context.Response.WriteAsync(json);
    }
}