# ADR-003: Agregado `Delivery` que protege sus invariantes

| Campo | Valor |
|---|---|
| **Estado** | Aceptada |
| **Fecha** | 29 de octubre de 2026 |
| **Decisor** | Andrés Barrera Claros |

## Contexto
Para asignar un repartidor hay reglas que siempre deben cumplirse (INV-01 a INV-08): un solo repartidor, solo entregas Pendientes, repartidor en línea y con cupo, etc. Hay que decidir **dónde viven esas reglas** y **cómo se representan los estados**.

## Opciones consideradas

### Dónde viven las invariantes
| Opción | Resultado |
|---|---|
| **A. En la entidad `Delivery`** (modelo de dominio rico) | **Elegida** |
| B. En el caso de uso (la entidad solo guarda datos) | Descartada: la regla se puede saltar desde otro caso de uso o desde una prueba. |
| C. En la base de datos (restricciones o disparadores) | Descartada: ata el negocio a la infraestructura y es difícil de probar. |
| D. El dominio consulta el puerto de disponibilidad directamente | Descartada: haría depender al dominio de la aplicación o de la infraestructura. |

### Cómo llega la disponibilidad al dominio
| Opción | Resultado |
|---|---|
| **A. Como objeto de valor `CourierAvailability` que el caso de uso obtiene del puerto y pasa a `AssignCourier`** | **Elegida** |
| B. Pasar solo el identificador del repartidor y que el dominio busque lo demás | Descartada: acopla el dominio a un puerto. |
| C. Verificar la disponibilidad en el caso de uso antes de llamar a la entidad | Descartada: las reglas INV-04 e INV-05 quedarían fuera del dominio. |

### Cómo se representan los estados
| Opción | Resultado |
|---|---|
| **A. Enumeración `DeliveryStatus` y transiciones como métodos de la entidad** | **Elegida** |
| B. Librería o máquina de estados finita | Descartada: no hay estados de interfaz que controlar (video 25) y agrega una dependencia sin necesidad. |
| C. Una clase por estado (patrón State) | Descartada por ahora: sobrediseño para 6 estados y pocas transiciones. |

## Decisión
`Delivery` es la **raíz del agregado**. Sus propiedades tienen **setters privados** y todo cambio pasa por `AssignCourier`, `ReleaseCourier` y `Cancel`, que validan las invariantes y lanzan excepciones de dominio con mensajes de negocio. El caso de uso obtiene un `CourierAvailability` desde un puerto y se lo entrega al método `AssignCourier`. Los estados son una enumeración.

## Justificación
- Las reglas **no se pueden saltar**: es imposible dejar una entrega Pendiente con repartidor, o Asignada sin él.
- El dominio sigue **puro**: recibe datos ya resueltos, no consulta nada.
- Las reglas se **prueban de forma directa** y rápida (`DeliveryTests`), sin infraestructura.
- Cumple lo que pide la actividad: "la entidad protege la invariante, el caso de uso coordina la intención".

## Consecuencias y trade-offs
| Trade-off | Qué aceptamos | Cómo lo manejamos |
|---|---|---|
| La disponibilidad es una foto en el tiempo | Entre que se consulta y se guarda, el repartidor podría llenarse. | Aceptable en esta etapa; se refuerza con control de concurrencia en la Actividad 4 (ver ADR-004). |
| El método de asignación recibe un objeto de valor extra | Firma un poco más larga. | A cambio, el dominio no depende de ningún puerto. |
| Excepciones para errores de negocio | Pueden ser costosas si se usan como flujo normal. | Los errores de negocio son excepcionales en este flujo; se traducen en la entrada. |
| Enumeración en lugar de patrón State | Si los estados crecen mucho, habrá que refactorizar. | Los criterios de reevaluación se revisarán en la Actividad 4. |

## Cómo se verifica
Escenarios E-01 a E-10 y pruebas de `DeliveryTests` (ver [documento 03](../03-invariantes-y-escenarios.md)).
