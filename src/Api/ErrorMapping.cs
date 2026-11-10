using PixelRoute.Application.Errors;
using PixelRoute.Domain.Common;
using PixelRoute.Domain.Deliveries;

namespace PixelRoute.Api;

/// <summary>Traduce los errores del negocio y de la aplicación a códigos HTTP. Solo la capa de entrada sabe de HTTP.</summary>
public static class ErrorMapping
{
    public static (int Status, string Code, string Title) Describe(Exception exception) => exception switch
    {
        DeliveryNotFoundException => (404, "delivery_not_found", "La entrega no existe"),
        CourierNotFoundException => (404, "courier_not_found", "El repartidor no existe"),
        DeliveryAlreadyAssignedException => (409, "delivery_already_assigned", "La entrega ya tiene repartidor"),
        InvalidDeliveryStateException => (409, "invalid_delivery_state", "El estado de la entrega no permite la operación"),
        ConcurrencyConflictException => (409, "concurrency_conflict", "La entrega fue modificada por otra operación"),
        CourierNotAvailableException => (422, "courier_not_available", "El repartidor no está disponible"),
        CourierRouteSaturatedException => (422, "courier_route_saturated", "La ruta del repartidor está saturada"),
        DomainException => (400, "domain_rule_violation", "Se incumplió una regla del negocio"),
        _ => (500, "unexpected_error", "Error inesperado")
    };
}
