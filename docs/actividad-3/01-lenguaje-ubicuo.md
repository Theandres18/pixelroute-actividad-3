# 01. Lenguaje ubicuo

El lenguaje ubicuo es el vocabulario común entre negocio y equipo técnico. Los mismos términos se usan en los documentos, en las conversaciones y en el código. Como el código está en inglés (siguiendo el ejemplo de la actividad), la tabla incluye el nombre en el código.

## Glosario

| Término (español) | En el código | Definición | Tipo |
|---|---|---|---|
| **Entrega** | `Delivery` | Trayecto que lleva un pedido desde la micro-bodega hasta el cliente. Es lo que se asigna a un repartidor. | Entidad (raíz del agregado) |
| **Pedido** | `OrderId` (solo la referencia) | Compra del cliente. Vive en el contexto Pedidos; la entrega solo guarda su identificador. | Entidad de otro contexto |
| **Repartidor** | `Courier` / `CourierId` | Persona independiente que realiza entregas. | Entidad (contexto Despacho y flota) |
| **Disponibilidad del repartidor** | `CourierAvailability` | Foto de si el repartidor está en línea y cuántas entregas activas lleva frente a su máximo. | Objeto de valor |
| **Dirección de entrega** | `DeliveryAddress` | Lugar donde se entrega el pedido. No puede estar vacía. | Objeto de valor |
| **Promesa de entrega** | `DeliveryPromise` | Hora límite que se le prometió al cliente (3 horas desde la creación). | Objeto de valor |
| **Estado de entrega** | `DeliveryStatus` | Etapa de la entrega: Pendiente, Asignada, En ruta, Entregada, Fallida o Cancelada. | Enumeración |
| **Asignar** | `AssignCourier` | Entregarle una entrega Pendiente a un repartidor concreto. | Comportamiento |
| **Liberar** | `ReleaseCourier` | Quitarle el repartidor a una entrega Asignada, que vuelve a Pendiente. | Comportamiento |
| **Entrega activa** | (se cuenta en `ActiveDeliveries`) | Entrega Asignada o En ruta de un repartidor. | Concepto |
| **Ruta saturada** | `IsRouteSaturated` | El repartidor ya lleva el máximo de entregas activas. | Regla |
| **Despachador** | (actor) | Persona o proceso que decide a quién se asigna cada entrega. | Actor |
| **Micro-bodega** | (contexto Inventario) | Punto de almacenamiento pequeño desde donde salen los pedidos. | Concepto |
| **Evento de dominio** | `IDomainEvent`, `DeliveryAssigned` | Hecho importante que ya ocurrió, por ejemplo "a la entrega X se le asignó el repartidor Y". | Concepto |
| **Puerto** | `IDeliveryRepository`, etc. | Interfaz que expresa lo que necesita un caso de uso. | Concepto técnico |
| **Adaptador** | `InMemoryDeliveryRepository`, etc. | Implementación concreta de un puerto. | Concepto técnico |

## Términos ambiguos que deben confirmarse con el negocio
Estos términos tienen más de una interpretación posible. En la propuesta tomé una decisión provisional (marcada como supuesto) y quedan por confirmar.

| Término ambiguo | Interpretaciones posibles | Supuesto que adopté | A confirmar con |
|---|---|---|---|
| **Repartidor disponible** | En línea; en línea y sin entrega activa; dentro de su turno; en línea y con cupo. | Disponible = **en línea y con cupo** en su ruta. | Operación / flota |
| **Ruta saturada** | Por número de entregas, por tiempo total, por distancia o por peso. | Saturada = entregas activas **≥ 3** (máximo asumido, S8). | Operación |
| **Entrega activa** | Solo En ruta; Asignada y En ruta. | Activa = **Asignada o En ruta**. | Operación |
| **Asignada** | Asignada por el sistema sin aceptación; asignada y aceptada por el repartidor. | Asignada = **el repartidor fue elegido**; la aceptación es un paso posterior (Actividad 4). | Producto |
| **Entrega** frente a **pedido** y **despacho** | Se usan como sinónimos en conversaciones. | **Pedido** = compra del cliente; **entrega** = trayecto; **despacho** = proceso de preparar y asignar. | Negocio |
| **Promesa de 3 horas** | Contada desde el pago, desde la confirmación o desde que sale de la bodega. | Contada **desde la creación de la entrega** (pago confirmado). | Negocio |
| **Zona de cobertura** | Radio fijo, barrios o polígono. | No se modela en este flujo. | Negocio |
| **Liberar repartidor** | Rechazo del repartidor; falla técnica; decisión del despachador. | Cualquiera de las tres usa la misma operación `ReleaseCourier`. | Operación |

## Nuevo supuesto de trabajo
Se suma a los supuestos S1 a S7 del [catálogo de la Actividad 2](../actividad-2/01-catalogo-de-requisitos.md):

| ID | Supuesto |
|---|---|
| **S8** | Un repartidor puede llevar como máximo **3 entregas activas** a la vez. |
