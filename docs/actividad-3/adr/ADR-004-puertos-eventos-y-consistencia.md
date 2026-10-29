# ADR-004: Puertos de salida, eventos de dominio y consistencia

| Campo | Valor |
|---|---|
| **Estado** | Aceptada |
| **Fecha** | 29 de octubre de 2026 |
| **Decisor** | Andrés Barrera Claros |

## Contexto
El caso de uso debe guardar la entrega y **avisar al resto del sistema** que se asignó un repartidor (para notificar al cliente y dar trazabilidad). También necesita la hora actual. Hay que decidir **cómo se publican los resultados** y qué se hace con el tiempo y la concurrencia.

## Opciones consideradas

### Cómo se avisa del resultado
| Opción | Resultado |
|---|---|
| **A. La entidad registra eventos de dominio y el caso de uso los publica por un puerto después de guardar** | **Elegida** |
| B. El caso de uso llama directamente a un servicio de notificaciones | Descartada: lo acopla a un proveedor concreto. |
| C. La entidad publica el evento por sí misma | Descartada: el dominio dependería de infraestructura. |
| D. Publicar antes de guardar | Descartada: se avisaría de algo que podría no persistirse. |

### Cómo se maneja el tiempo
| Opción | Resultado |
|---|---|
| **A. Puerto `IClock` y el caso de uso pasa la hora al dominio** | **Elegida** |
| B. Usar `DateTimeOffset.UtcNow` dentro de la entidad | Descartada: pruebas no deterministas y dominio atado al reloj del sistema. |

### Consistencia entre guardar y publicar
| Opción | Resultado |
|---|---|
| **A. Guardar, luego publicar; la salida confiable de eventos queda para la Actividad 4** | **Elegida** |
| B. Implementar ahora una bandeja de salida (outbox) | Descartada por ahora: requiere persistencia real, que esta actividad no pide. |
| C. Transacciones distribuidas | Descartada: complejidad innecesaria en un monolito. |

## Decisión
- La entidad **acumula** eventos de dominio (`DeliveryAssigned`).
- El caso de uso, **después de guardar**, los entrega al puerto `IDomainEventPublisher` y limpia la lista.
- La hora se obtiene del puerto `IClock` y se pasa al dominio como parámetro.
- Si una regla falla, la excepción corta el flujo **antes** de guardar y de publicar, así que no queda nada a medias.
- Los adaptadores de esta actividad son en memoria; los reales se harán en la Actividad 4.

## Justificación
- El dominio queda **libre de infraestructura y de reloj**, y las pruebas son deterministas.
- El caso de uso solo depende de interfaces (D de SOLID).
- Cambiar de proveedor de notificaciones o de bus de mensajes toca solo un adaptador.

## Consecuencias, riesgos y trade-offs
| Riesgo o trade-off | Detalle | Mitigación |
|---|---|---|
| **Doble escritura**: se guarda pero falla la publicación | El cliente podría no recibir el aviso, aunque la asignación sí quedó. | Salida confiable de eventos (outbox) y reintentos en la Actividad 4 (QS-08). |
| **Concurrencia**: dos asignaciones simultáneas de la misma entrega | INV-01 podría violarse entre la lectura y el guardado. | Control de versión optimista en el repositorio real (Actividad 4); se probará con un escenario concurrente. |
| Más piezas para una operación simple | Puerto de reloj, de eventos, etc. | Son pequeñas y habilitan pruebas sin infraestructura. |
| Eventos en memoria no sobreviven a un reinicio | Solo para pruebas y desarrollo. | Se reemplaza el adaptador. |

## Cómo se verifica
Pruebas `AssignCourierUseCaseTests` (guarda y publica solo si todo sale bien, E-04, E-08 y E-11) y `AssignCourierFlowTests` con los adaptadores en memoria.
