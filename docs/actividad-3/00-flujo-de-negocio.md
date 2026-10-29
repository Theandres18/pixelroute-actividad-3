# 00. Flujo de negocio elegido: asignar un repartidor a una entrega

## Por qué este flujo
Elegí **asignar una entrega** porque es el punto donde se juegan los riesgos más importantes que identifiqué en las actividades 1 y 2: la promesa de entrega en 3 horas (R3), la disponibilidad variable de los repartidores independientes (R10), la coordinación entre áreas (R5) y la trazabilidad (R4). Además es acotado: tiene pocos actores, reglas claras y un resultado verificable, así que permite diseñar el dominio con profundidad sin intentar resolver todo el sistema.

Corresponde a los requisitos **RF-10** (asignar automáticamente al repartidor) y **RF-11** (el repartidor acepta o rechaza), y al escenario de calidad **QS-05** (primera oferta en menos de 60 segundos) de la [Actividad 2](../actividad-2/03-escenarios-de-calidad.md).

## Propósito
Dejar cada entrega lista para despacho en manos de **un solo repartidor disponible y con capacidad**, de forma que se pueda cumplir la promesa de 3 horas al cliente.

## Actores
| Actor | Participación |
|---|---|
| **Despachador** (administrador de almacén o el propio sistema de despacho) | Inicia la asignación, de forma manual o automática. |
| **Repartidor independiente** | Es el recurso que se asigna. Más adelante acepta o rechaza. |
| **Cliente** | Recibe un aviso de que su pedido ya tiene repartidor. |
| **Contexto Pedidos** | Origen de la entrega: avisa que el pedido está listo para despacho. |
| **Sistema de notificaciones** (externo) | Entrega el aviso al cliente. |

## Precondiciones
- Existe una entrega en estado **Pendiente** (el pedido ya tiene pago confirmado y stock reservado).
- Existe el repartidor y se puede consultar su disponibilidad.

## Flujo principal
1. El despachador (o el sistema) solicita asignar un repartidor a una entrega.
2. El sistema carga la entrega y verifica que exista.
3. El sistema consulta la disponibilidad del repartidor a través de un puerto.
4. La entrega aplica sus reglas: no tiene otro repartidor, está Pendiente, el repartidor está en línea y su ruta no está saturada.
5. La entrega queda **Asignada** al repartidor.
6. El sistema guarda el cambio.
7. El sistema publica el hecho "entrega asignada", que se usa para avisar al cliente y para el seguimiento.

## Flujos alternos (errores de negocio)
| Caso | Resultado |
|---|---|
| La entrega no existe | Se rechaza; no se guarda ni se publica nada. |
| El repartidor no existe | Se rechaza; no se guarda ni se publica nada. |
| La entrega ya tiene repartidor | Se rechaza con un error claro. |
| La entrega no está Pendiente (por ejemplo, cancelada) | Se rechaza. |
| El repartidor está fuera de línea | Se rechaza. |
| La ruta del repartidor está saturada | Se rechaza; el despachador debe elegir otro. |

## Resultado esperado
Una entrega en estado **Asignada**, con exactamente un repartidor, la hora de asignación registrada y un evento `DeliveryAssigned` publicado. Si algo falla, **nada cambia**.

## Fuera del alcance de este flujo (se verá en la Actividad 4)
- Elegir automáticamente al "mejor" repartidor por cercanía o calificación.
- Que el repartidor acepte o rechace desde su app.
- Persistencia real, notificaciones reales e integración con geolocalización.
