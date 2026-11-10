using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace PixelRoute.Infrastructure.Notifications;

/// <summary>
/// Sustituto del sistema externo de notificaciones, que no está disponible en este entorno.
/// Modos: "Ok" (siempre envía), "AlwaysFail" (siempre falla) y "FailTwiceThenOk" (falla dos veces y luego envía).
/// </summary>
public sealed class SimulatedNotificationGateway : INotificationGateway
{
    private readonly string _mode;
    private readonly ILogger<SimulatedNotificationGateway> _logger;
    private readonly ConcurrentDictionary<Guid, int> _attempts = new();

    public SimulatedNotificationGateway(string mode, ILogger<SimulatedNotificationGateway> logger)
    {
        _mode = mode;
        _logger = logger;
    }

    public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var attempt = _attempts.AddOrUpdate(message.DeliveryId, 1, (_, current) => current + 1);

        if (_mode == "AlwaysFail")
        {
            throw new NotificationUnavailableException("Servicio de notificaciones no disponible (simulado).");
        }

        if (_mode == "FailTwiceThenOk" && attempt <= 2)
        {
            throw new NotificationUnavailableException("Servicio de notificaciones no disponible (simulado).");
        }

        _logger.LogInformation("Aviso simulado enviado para la entrega {DeliveryId}", message.DeliveryId);
        return Task.CompletedTask;
    }
}
