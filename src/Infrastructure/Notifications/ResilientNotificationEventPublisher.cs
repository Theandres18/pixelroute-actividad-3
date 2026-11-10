using System.Text.Json;
using Microsoft.Extensions.Logging;
using PixelRoute.Application.Abstractions;
using PixelRoute.Application.Contracts;
using PixelRoute.Domain.Common;
using PixelRoute.Domain.Deliveries;
using PixelRoute.Infrastructure.Observability;

namespace PixelRoute.Infrastructure.Notifications;

/// <summary>
/// Adaptador del puerto IDomainEventPublisher hacia el sistema de notificaciones.
/// Reintenta con espera exponencial y, si se agotan los intentos, guarda el mensaje en la cola de mensajes fallidos.
/// Nunca lanza una excepción por una falla del servicio externo: la asignación ya quedó guardada.
/// </summary>
public sealed class ResilientNotificationEventPublisher : IDomainEventPublisher
{
    private readonly INotificationGateway _gateway;
    private readonly IDeadLetterStore _deadLetters;
    private readonly IClock _clock;
    private readonly NotificationRetryOptions _options;
    private readonly ILogger<ResilientNotificationEventPublisher> _logger;

    public ResilientNotificationEventPublisher(
        INotificationGateway gateway,
        IDeadLetterStore deadLetters,
        IClock clock,
        NotificationRetryOptions options,
        ILogger<ResilientNotificationEventPublisher> logger)
    {
        _gateway = gateway;
        _deadLetters = deadLetters;
        _clock = clock;
        _options = options;
        _logger = logger;
    }

    public async Task PublishAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            if (domainEvent is DeliveryAssigned assigned)
            {
                var message = new NotificationMessage(
                    assigned.DeliveryId.Value,
                    assigned.CourierId.Value,
                    assigned.OccurredAt);

                await SendWithRetriesAsync(message, cancellationToken);
            }
        }
    }

    private async Task SendWithRetriesAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(1, _options.MaxAttempts);
        var attempts = 0;
        var lastError = string.Empty;

        while (attempts < maxAttempts)
        {
            attempts++;

            try
            {
                await _gateway.SendAsync(message, cancellationToken);
                PixelRouteTelemetry.NotificationsSent.Add(1);
                _logger.LogInformation(
                    "Aviso enviado para la entrega {DeliveryId} en el intento {Attempt}",
                    message.DeliveryId,
                    attempts);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Se registra solo el tipo de error, nunca el contenido del mensaje.
                lastError = ex.GetType().Name;
                PixelRouteTelemetry.NotificationRetries.Add(1);
                _logger.LogWarning(
                    "Falló el envío del aviso de la entrega {DeliveryId} (intento {Attempt} de {MaxAttempts}): {ErrorType}",
                    message.DeliveryId,
                    attempts,
                    maxAttempts,
                    lastError);

                if (attempts < maxAttempts)
                {
                    var delay = TimeSpan.FromMilliseconds(_options.BaseDelayMilliseconds * Math.Pow(2, attempts - 1));
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        var deadLetter = new DeadLetter(
            Guid.NewGuid(),
            nameof(NotificationMessage),
            JsonSerializer.Serialize(message),
            lastError,
            attempts,
            _clock.UtcNow);

        await _deadLetters.AddAsync(deadLetter, cancellationToken);
        PixelRouteTelemetry.NotificationsDeadLettered.Add(1);
        _logger.LogError(
            "El aviso de la entrega {DeliveryId} pasó a la cola de mensajes fallidos tras {Attempts} intentos",
            message.DeliveryId,
            attempts);
    }
}
