namespace PixelRoute.Domain.Deliveries;

/// <summary>Referencia al pedido (que vive en el contexto Pedidos). Solo se guarda su identidad.</summary>
public sealed record OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
