# ADR-007: Adaptador de notificaciones con reintentos y cola de mensajes fallidos

| Campo | Valor |
|---|---|
| **Estado** | Aceptada |
| **Fecha** | 29 de octubre de 2026 |
| **Decisor** | Andrés Barrera Claros |
| **Relacionada con** | [ADR-004](../../actividad-3/adr/ADR-004-puertos-eventos-y-consistencia.md) |

## Contexto
Cuando se asigna un repartidor hay que avisar al cliente. El sistema de notificaciones es **externo** y puede fallar (QS-08). En este entorno no está disponible, así que hay que decidir cómo se integra, qué se hace ante una falla y cómo se prueba sin el servicio real.

## Opciones consideradas
| Opción | Resultado |
|---|---|
| A. Sin reintentos: si falla, se pierde el aviso | Descartada: se pierde información sin dejar rastro. |
| B. Si falla el aviso, fallar la asignación | Descartada: se desharía algo que ya quedó guardado y bloquearía la operación por un servicio auxiliar. |
| C. Reintentar sin límite | Descartada: puede saturar el servicio y bloquear la solicitud. |
| **D. Reintentos limitados con espera exponencial y cola de mensajes fallidos** | **Elegida** |
| E. Bandeja de salida (outbox) transaccional con un proceso en segundo plano | Descartada por ahora: es lo más robusto, pero agrega un proceso, estados y más código. Queda como próximo paso. |
| F. Cola externa (RabbitMQ u otra) | Descartada: infraestructura adicional que no se justifica para este volumen. |

## Decisión
- `ResilientNotificationEventPublisher` implementa `IDomainEventPublisher`. Traduce `DeliveryAssigned` en un `NotificationMessage` (solo identificadores) y lo envía por `INotificationGateway`.
- Hasta **3 intentos**, con espera de 200, 400 y 800 ms (configurable).
- Si todos fallan, guarda el mensaje en la **cola de mensajes fallidos** (`dead_letter_messages`) y registra un error. **No lanza una excepción**: la asignación ya quedó guardada.
- El servicio externo se sustituye por `SimulatedNotificationGateway`, con tres modos: `Ok`, `AlwaysFail` y `FailTwiceThenOk`. Se elige con `Notifications:Mode`.
- Operaciones puede revisar los mensajes fallidos en `GET /api/v1/admin/dead-letters`.

## Justificación
- Cumple QS-08: la asignación continúa aunque el servicio de avisos caiga.
- Ningún aviso se pierde en silencio: o se envía, o queda en la cola.
- El dominio y el caso de uso no conocen los reintentos ni los avisos: todo está en el adaptador.

## Trade-offs y riesgos
| Riesgo o trade-off | Detalle | Mitigación |
|---|---|---|
| **Doble escritura (RO-02)** | Si el proceso se cae entre guardar la entrega y publicar el evento, el aviso se pierde sin pasar por la cola. | Riesgo residual. Solución prevista: outbox transaccional. |
| La solicitud espera los reintentos | En el peor caso, la respuesta tarda unos 1,4 s más. | Tiempos configurables; con outbox el envío pasaría a segundo plano. |
| Los mensajes fallidos no se reenvían solos | Hay que revisarlos a mano. | Se pueden listar; el reenvío automático es un próximo paso. |
| El servicio es simulado | No se probó contra un proveedor real. | La frontera `INotificationGateway` permite reemplazarlo; el comportamiento ante fallas está probado. |

## Verificación
`ResilientNotificationEventPublisherTests`, y en la API `Si_el_servicio_de_avisos_cae_la_asignacion_sigue...`. Experimento de caos en el [documento 03](../03-observabilidad.md).
