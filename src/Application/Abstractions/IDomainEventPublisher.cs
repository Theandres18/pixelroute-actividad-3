using PixelRoute.Domain.Common;

namespace PixelRoute.Application.Abstractions;

/// <summary>Puerto de salida: publica o registra los eventos de dominio resultantes de un caso de uso.</summary>
public interface IDomainEventPublisher
{
    Task PublishAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default);
}
