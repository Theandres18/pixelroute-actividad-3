using System.Security.Cryptography;
using System.Text;

namespace PixelRoute.Api.Middleware;

/// <summary>
/// Protege los endpoints de operación con una clave de despachador enviada en el encabezado X-Api-Key.
/// Representa, de forma simplificada, el contexto Identidad y accesos. Si la clave no está configurada,
/// rechaza todo (falla cerrada). La comparación es de tiempo constante.
/// </summary>
public sealed class ApiKeyEndpointFilter : IEndpointFilter
{
    private const string HeaderName = "X-Api-Key";

    private readonly IConfiguration _configuration;

    public ApiKeyEndpointFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var expected = _configuration["Security:DispatcherApiKey"];
        var provided = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrEmpty(expected) || !AreEqual(expected, provided))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "No autorizado",
                detail: "Falta la clave X-Api-Key o es inválida.");
        }

        return await next(context);
    }

    private static bool AreEqual(string expected, string provided)
        => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(provided));
}
