using System.Diagnostics;
using PixelRoute.Api.Contracts;
using PixelRoute.Api.Middleware;
using PixelRoute.Application.Contracts;
using PixelRoute.Application.UseCases;
using PixelRoute.Infrastructure.Observability;

namespace PixelRoute.Api.Endpoints;

/// <summary>
/// Capa de entrada: recibe HTTP, valida el formato de los datos, invoca el caso de uso y devuelve la respuesta.
/// No contiene reglas de negocio ni acceso a datos.
/// </summary>
public static class DeliveryEndpoints
{
    public static IEndpointRouteBuilder MapDeliveryEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/v1").AddEndpointFilter<ApiKeyEndpointFilter>();

        api.MapPost("/deliveries", CreateDeliveryAsync);
        api.MapGet("/deliveries/{id:guid}", GetDeliveryAsync);
        api.MapPost("/deliveries/{id:guid}/assign-courier", AssignCourierAsync);
        api.MapGet("/admin/dead-letters", ListDeadLettersAsync);

        return app;
    }

    private static async Task<IResult> CreateDeliveryAsync(
        CreateDeliveryRequest request,
        CreateDeliveryUseCase useCase,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.OrderId == Guid.Empty)
        {
            errors["orderId"] = new[] { "El identificador del pedido es obligatorio." };
        }

        if (string.IsNullOrWhiteSpace(request.Address))
        {
            errors["address"] = new[] { "La dirección es obligatoria." };
        }
        else if (request.Address.Length > 200)
        {
            errors["address"] = new[] { "La dirección no puede superar los 200 caracteres." };
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var result = await useCase.ExecuteAsync(
            new CreateDeliveryCommand(request.OrderId, request.Address!),
            cancellationToken);

        logger.LogInformation("Entrega {DeliveryId} creada", result.DeliveryId);
        return Results.Created($"/api/v1/deliveries/{result.DeliveryId}", result);
    }

    private static async Task<IResult> GetDeliveryAsync(
        Guid id,
        GetDeliveryUseCase useCase,
        CancellationToken cancellationToken)
    {
        var view = await useCase.ExecuteAsync(id, cancellationToken);
        return Results.Ok(view);
    }

    private static async Task<IResult> AssignCourierAsync(
        Guid id,
        AssignCourierRequest request,
        AssignCourierUseCase useCase,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (request.CourierId == Guid.Empty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["courierId"] = new[] { "El identificador del repartidor es obligatorio." }
            });
        }

        var stopwatch = Stopwatch.StartNew();
        using var activity = PixelRouteTelemetry.ActivitySource.StartActivity("AssignCourier");
        activity?.SetTag("delivery.id", id.ToString());

        var outcome = "ok";

        try
        {
            var result = await useCase.ExecuteAsync(new AssignCourierCommand(id, request.CourierId), cancellationToken);
            logger.LogInformation(
                "Repartidor {CourierId} asignado a la entrega {DeliveryId}",
                result.CourierId,
                result.DeliveryId);
            return Results.Ok(result);
        }
        catch (Exception exception)
        {
            outcome = ErrorMapping.Describe(exception).Code;
            logger.LogWarning("Asignación rechazada para la entrega {DeliveryId}: {ErrorCode}", id, outcome);
            throw;
        }
        finally
        {
            PixelRouteTelemetry.AssignmentsTotal.Add(1, new KeyValuePair<string, object?>("result", outcome));
            PixelRouteTelemetry.AssignmentDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private static async Task<IResult> ListDeadLettersAsync(
        ListDeadLettersUseCase useCase,
        int? limit,
        CancellationToken cancellationToken)
    {
        var items = await useCase.ExecuteAsync(limit ?? 50, cancellationToken);
        return Results.Ok(items);
    }
}
