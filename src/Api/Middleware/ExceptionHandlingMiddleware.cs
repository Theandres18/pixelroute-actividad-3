using Microsoft.AspNetCore.Mvc;

namespace PixelRoute.Api.Middleware;

/// <summary>
/// Convierte las excepciones en respuestas claras (ProblemDetails). Para errores inesperados
/// responde un mensaje genérico: nunca devuelve el texto de la excepción ni la traza.
/// </summary>
public sealed class ExceptionHandlingMiddleware
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
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            var (status, code, title) = ErrorMapping.Describe(exception);

            if (status >= 500)
            {
                _logger.LogError(exception, "Error inesperado procesando {Method} {Path}", context.Request.Method, context.Request.Path.Value);
            }
            else
            {
                _logger.LogInformation("Solicitud rechazada ({ErrorCode}) en {Path}", code, context.Request.Path.Value);
            }

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status >= 500
                    ? "Ocurrió un error inesperado. Indique el identificador de correlación al equipo de soporte."
                    : exception.Message
            };
            problem.Extensions["errorCode"] = code;
            problem.Extensions["correlationId"] = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();

            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
