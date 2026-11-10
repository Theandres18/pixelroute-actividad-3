namespace PixelRoute.Infrastructure.Notifications;

/// <summary>Aviso de que una entrega tiene repartidor. Solo lleva identificadores: el sistema de notificaciones resuelve el destinatario.</summary>
public sealed record NotificationMessage(Guid DeliveryId, Guid CourierId, DateTimeOffset OccurredAt);
