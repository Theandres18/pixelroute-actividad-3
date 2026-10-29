# 08. Ejemplo ilustrativo: contratos y casos de uso con puertos

Este documento muestra **cómo se vería la decisión del ADR-001 dentro de un módulo**, usando el ejemplo que presentó el profesor en los videos. Es un ejemplo ilustrativo en C#: **no es una decisión de tecnología** (RES-06) y el código no está compilado ni forma parte de la entrega funcional. Sirve para explicar los conceptos de acoplamiento, cohesión y contratos (videos 05 y 08).

## Dos formas de resolver "crear un pedido"

**Opción A: mantener `OrderService` y agregar condiciones.** Es la solución rápida: se agregan `if`, `try/catch` y métodos privados. El costo es que la clase acumula responsabilidades y cada prueba necesita infraestructura real o mocks complejos. Es el equivalente a la opción A de capas de nuestra comparación.

**Opción B: separar el caso de uso de sus dependencias.** El caso de uso conserva la regla de negocio y recibe **puertos** (interfaces) para lo demás. Cada componente tiene una sola razón para cambiar. Es lo que adopta el ADR-001.

## Los puertos (contratos)
```csharp
public interface IRouteEstimator
{
    Task<RouteEstimate> EstimateAsync(string address);
}

public interface IOrderRepository
{
    Task SaveAsync(Order order);
}

public interface IOrderNotifier
{
    Task NotifyCreatedAsync(Order order);
}
```

## El caso de uso
```csharp
public sealed class CreateOrderUseCase
{
    private readonly IRouteEstimator _routes;
    private readonly IOrderRepository _orders;
    private readonly IOrderNotifier _notifier;

    public CreateOrderUseCase(
        IRouteEstimator routes,
        IOrderRepository orders,
        IOrderNotifier notifier)
    {
        _routes = routes;
        _orders = orders;
        _notifier = notifier;
    }

    public async Task ExecuteAsync(string email, decimal total, string address)
    {
        var order = Order.Create(email, total);
        order.AssignRoute(await _routes.EstimateAsync(address));
        await _orders.SaveAsync(order);
        await _notifier.NotifyCreatedAsync(order);
    }
}
```

## Cómo se conecta con la arquitectura de PixelRoute
| Puerto | Adaptador (infraestructura) | Relación con la decisión |
|---|---|---|
| `IRouteEstimator` | Adaptador de geolocalización | Si el servicio de mapas cambia o cae, solo cambia el adaptador (QS-08). |
| `IOrderRepository` | Persistencia del módulo Pedidos | El módulo es dueño de sus datos (CV-03). |
| `IOrderNotifier` | Adaptador de notificaciones | Un cambio de proveedor no toca la lógica (RNF-11). |

Para que el módulo Pedidos hable con Inventario sin acoplarse, se agregaría otro puerto, que es el **contrato entre módulos**:

```csharp
public interface IInventoryReservation
{
    Task<ReservationResult> ReserveAsync(string sku, int quantity, string hubId);
}
```

## Por qué ayuda a los requisitos
- **QS-15 (testabilidad):** las pruebas usan implementaciones falsas de los puertos, sin base de datos ni red.
- **QS-12 y QS-13 (evolución):** reemplazar un adaptador o agregar uno nuevo no modifica el caso de uso.
- **CV-04:** se puede comprobar que el caso de uso no referencia infraestructura concreta.

Ejemplo de prueba (ilustrativo):
```csharp
[Fact]
public async Task Guarda_el_pedido_y_notifica()
{
    var orders = new FakeOrderRepository();
    var notifier = new FakeOrderNotifier();
    var useCase = new CreateOrderUseCase(new FakeRouteEstimator(), orders, notifier);

    await useCase.ExecuteAsync("cliente@correo.com", 50000m, "Calle 1 # 2-3");

    Assert.Single(orders.Saved);
    Assert.Single(notifier.Notified);
}
```

## Aspectos a mejorar en el diseño real
El ejemplo es didáctico. Al diseñar el módulo de verdad habría que:
- Validar el inventario antes de confirmar el pedido (puerto `IInventoryReservation`).
- Decidir qué pasa si el pedido se guarda pero falla la notificación: no debe deshacerse el pedido; el aviso se reintenta (QS-08).
- Devolver un resultado en lugar de `Task` vacío, para saber qué ocurrió.
- Validar los datos de entrada y la autorización del acudiente (RF-22).

Estas decisiones se tomarán en las siguientes unidades.
