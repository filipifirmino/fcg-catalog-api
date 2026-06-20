
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace FCG_CATALOG_API.Api;
public class GlobalExceptionHandler
{
    private const string STANDARD_MESSAGE = "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.";
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env; 
    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception,
        "Erro Crítico detectado: {Message} | Path: {Path} | User: {User}",
        exception.Message,
        context.Request.Path,
        context.User.Identity?.Name ?? "Anonymous");

        context.Response.ContentType = "application/json";
        var errorMessage = _env.IsDevelopment() ?
            (exception.InnerException?.Message ?? exception.Message ?? STANDARD_MESSAGE)
            : STANDARD_MESSAGE;

        if(context != null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var problem = new ProblemDetails
            {
                Status = context.Response.StatusCode,
                Title = "Erro Interno",
                Detail = errorMessage,
                Instance = context.Request.Path
            };
            var json = JsonSerializer.Serialize(problem);
            await context.Response.WriteAsync(json);
        }

    }
}