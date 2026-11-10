namespace PixelRoute.Infrastructure.Notifications;

/// <summary>Frontera hacia el sistema externo de notificaciones. Se puede reemplazar sin tocar el resto del sistema.</summary>
public interface INotificationGateway
{
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
