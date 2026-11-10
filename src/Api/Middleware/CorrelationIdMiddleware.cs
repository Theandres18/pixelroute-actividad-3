namespace PixelRoute.Api.Middleware;

/// <summary>
/// Asigna a cada solicitud un identificador de correlación (X-Correlation-Id), lo devuelve en la respuesta
/// y lo agrega a todos los registros de esa solicitud. Solo acepta valores seguros (letras, números, guiones)
/// para evitar la inyección de texto en los registros.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var candidate = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = candidate is not null && IsValid(candidate)
            ? candidate
            : Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = correlationId;

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }

    private static bool IsValid(string value)
        => value.Length is > 0 and <= 64 && value.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
}
