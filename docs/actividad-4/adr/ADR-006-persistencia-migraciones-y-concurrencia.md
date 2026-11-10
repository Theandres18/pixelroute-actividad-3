# ADR-006: SQLite, migraciones SQL versionadas y concurrencia optimista

| Campo | Valor |
|---|---|
| **Estado** | Aceptada |
| **Fecha** | 29 de octubre de 2026 |
| **Decisor** | Andrés Barrera Claros |
| **Relacionada con** | [ADR-003](../../actividad-3/adr/ADR-003-agregado-delivery-e-invariantes.md) y [ADR-004](../../actividad-3/adr/ADR-004-puertos-eventos-y-consistencia.md) |

## Contexto
La Actividad 2 dejó la base de datos "por definir". Hay que elegir cómo se persiste el estado, cómo se versiona el esquema y cómo se resuelve el riesgo de concurrencia que dejó abierto el ADR-004 (dos asignaciones simultáneas de la misma entrega).

## Opciones consideradas

### Motor de base de datos
| Opción | Resultado |
|---|---|
| **A. SQLite** (archivo local) | **Elegida** |
| B. PostgreSQL | Descartada por ahora: exige instalar y configurar un servidor para que el profesor pueda ejecutar el proyecto. Es el siguiente paso natural. |
| C. En memoria | Descartada: no demuestra persistencia ni migraciones. |

### Acceso a datos y migraciones
| Opción | Resultado |
|---|---|
| **A. SQL directo con consultas parametrizadas y scripts SQL versionados con un ejecutor propio** | **Elegida** |
| B. Entity Framework Core con migraciones | Descartada: oculta el SQL y los límites entre dominio y persistencia, y exige generar migraciones con una herramienta adicional. |
| C. Flyway o DbUp | Descartada: Flyway es de Java, y DbUp agrega un paquete más para un caso muy simple. |

### Concurrencia
| Opción | Resultado |
|---|---|
| **A. Control optimista con una columna `version`** | **Elegida** |
| B. Bloqueos pesimistas | Descartada: SQLite no los ofrece por fila y complicaría el flujo. |
| C. No controlar | Descartada: violaría la invariante INV-01 bajo concurrencia. |

### Estado durable o eventos
| Opción | Resultado |
|---|---|
| **A. Estado durable (se guarda el estado actual)** | **Elegida** |
| B. Event Sourcing | Descartada: agrega reconstrucción y versionado de eventos sin que el flujo lo necesite. |

## Decisión
1. Las entregas, los repartidores y los mensajes fallidos se guardan en **SQLite**, detrás de los puertos definidos en la Actividad 3.
2. El esquema se crea con **scripts SQL versionados** (`V001__create_schema.sql`) embebidos en el ensamblado y aplicados por `SqlMigrationRunner`: en orden, una sola vez, dentro de una transacción por script y registrados en `schema_migrations`. Los datos de demostración están en una carpeta aparte (`Seed`) y solo se cargan si `Database:SeedDemoData` es `true`.
3. Las reglas más importantes también están como **restricciones de la base de datos** (defensa en profundidad): una entrega Pendiente o Cancelada no puede tener repartidor, una Asignada o En ruta siempre lo tiene, y la dirección no puede estar vacía.
4. `SaveAsync` inserta si `Version == 0` y, si no, actualiza con `WHERE version = @version`. Si no se actualiza ninguna fila, lanza `ConcurrencyConflictException` (que la API traduce a 409).
5. El dominio incorpora `Delivery.Restore` (reconstruye verificando INV-03) y `Delivery.Version`, que solo gestiona el repositorio.

## Justificación
- Cualquiera puede ejecutar el proyecto sin instalar servidores.
- El SQL parametrizado evita la inyección de SQL y deja visible lo que se ejecuta.
- Las migraciones son **reproducibles**: una base nueva queda igual en cualquier máquina, y ejecutar el ejecutor dos veces no repite nada.
- El control optimista resuelve el riesgo de concurrencia del ADR-004 sin bloquear.

## Trade-offs
| Trade-off | Qué aceptamos | Cómo lo manejamos |
|---|---|---|
| SQLite escribe de a un proceso a la vez | No sirve para varias instancias ni para picos fuertes (QS-02). | Los puertos permiten cambiar a PostgreSQL sin tocar el dominio ni los casos de uso. Queda como próximo paso, con una prueba de carga. |
| Ejecutor de migraciones propio | No tiene sumas de verificación ni reversa. | Los scripts aplicados no se editan: los cambios van en un script nuevo. |
| `Version` y `Restore` en el dominio | Es una concesión técnica en una entidad pura. | Son pequeños, están documentados y `Version` solo la modifica el repositorio. |
| El control optimista no cubre todo | Dos entregas **distintas** asignadas al mismo repartidor a la vez podrían superar su cupo. | Riesgo residual RO-12, documentado, con la mitigación propuesta. |

## Verificación
`SqlMigrationRunnerTests` (idempotencia y restricciones), `SqliteDeliveryRepositoryTests` (lectura, escritura, conflicto concurrente y texto malicioso), `DeliveryRestoreTests`.
