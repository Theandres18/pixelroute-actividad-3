namespace PixelRoute.Domain.Common;

/// <summary>Hecho relevante del negocio que ya ocurrió.</summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
