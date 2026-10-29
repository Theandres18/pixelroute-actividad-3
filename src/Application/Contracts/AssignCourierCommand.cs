namespace PixelRoute.Application.Contracts;

/// <summary>Contrato de entrada del caso de uso. Usa tipos simples para no acoplar a quien lo invoque.</summary>
public sealed record AssignCourierCommand(Guid DeliveryId, Guid CourierId);
