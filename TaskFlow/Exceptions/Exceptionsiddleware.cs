using System.Text.Json;

namespace TaskFlow.Exceptions;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (BusinessException ex)
        {
            _logger.LogWarning(
                ex,
                "BusinessException oluştu. Path: {Path}",
                context.Request.Path);
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            var response = new
            {
                message = ex.Message
            };
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Beklenmeyen bir hata oluştu. Path: {Path}",
                context.Request.Path);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var response = new
            {
                message = "Beklenmeyen bir hata oluştu."
            };
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response)
            );
        }
    }
}