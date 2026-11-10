using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace PixelRoute.Infrastructure.Observability;

/// <summary>
/// Señales de observabilidad de PixelRoute: métricas y trazas. Ninguna lleva datos personales:
/// solo identificadores técnicos y resultados.
/// </summary>
public static class PixelRouteTelemetry
{
    public const string Name = "PixelRoute";

    public static readonly Meter Meter = new(Name, "1.0.0");

    public static readonly ActivitySource ActivitySource = new(Name, "1.0.0");

    public static readonly Counter<long> AssignmentsTotal = Meter.CreateCounter<long>(
        "pixelroute.assignments.total",
        description: "Solicitudes de asignación de repartidor, por resultado.");

    public static readonly Histogram<double> AssignmentDuration = Meter.CreateHistogram<double>(
        "pixelroute.assignments.duration",
        unit: "ms",
        description: "Duración de la asignación de repartidor.");

    public static readonly Counter<long> NotificationsSent = Meter.CreateCounter<long>(
        "pixelroute.notifications.sent",
        description: "Avisos enviados correctamente.");

    public static readonly Counter<long> NotificationRetries = Meter.CreateCounter<long>(
        "pixelroute.notifications.retries",
        description: "Intentos fallidos de envío de avisos.");

    public static readonly Counter<long> NotificationsDeadLettered = Meter.CreateCounter<long>(
        "pixelroute.notifications.dead_lettered",
        description: "Avisos enviados a la cola de mensajes fallidos.");
}
