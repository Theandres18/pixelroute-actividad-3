# 00. Qué se implementó y cómo se conecta con el diseño

> Proyecto académico y ficticio, inspirado en la comunidad de Roblox. No está afiliado a Roblox Corporation.

## El vertical slice
En la Actividad 3 diseñé el flujo **asignar un repartidor a una entrega**. En esta actividad lo implementé de punta a punta: una solicitud HTTP entra por la API, pasa por el caso de uso, la entidad protege las reglas, el resultado se guarda en SQLite y se avisa al cliente por un servicio de notificaciones, con manejo de errores en cada paso.

```mermaid
sequenceDiagram
    participant C as Cliente HTTP
    participant A as API (entrada)
    participant U as AssignCourierUseCase
    participant R as SqliteDeliveryRepository
    participant V as SqliteCourierAvailabilityProvider
    participant D as Delivery (dominio)
    participant N as ResilientNotificationEventPublisher

    C->>A: POST /api/v1/deliveries/{id}/assign-courier (X-Api-Key)
    A->>A: Valida clave y formato
    A->>U: ExecuteAsync(AssignCourierCommand)
    U->>R: GetAsync(deliveryId)
    R-->>U: Delivery (Restore con versión)
    U->>V: GetAvailabilityAsync(courierId)
    V-->>U: CourierAvailability
    U->>D: AssignCourier(availability, now)
    Note over D: Protege INV-01, 02, 04 y 05
    U->>R: SaveAsync (UPDATE con control de versión)
    U->>N: PublishAsync(DeliveryAssigned)
    N->>N: Hasta 3 intentos; si fallan, cola de fallidos
    U-->>A: AssignCourierResult
    A-->>C: 200 OK (o 404, 409, 422 según el error)
```

## Endpoints
| Método y ruta | Caso de uso | Respuestas |
|---|---|---|
| `POST /api/v1/deliveries` | `CreateDeliveryUseCase` | 201, 400, 401 |
| `GET /api/v1/deliveries/{id}` | `GetDeliveryUseCase` | 200, 401, 404 |
| `POST /api/v1/deliveries/{id}/assign-courier` | **`AssignCourierUseCase`** | 200, 400, 401, 404, 409, 422 |
| `GET /api/v1/admin/dead-letters` | `ListDeadLettersUseCase` | 200, 401 |
| `GET /health` | (público) | 200 |

## Trazabilidad: del diseño de la Actividad 3 a la implementación
| Diseño (Actividad 3) | Implementación | Archivo | Evidencia (prueba) |
|---|---|---|---|
| Entidad `Delivery` con invariantes INV-01 a INV-08 | Igual que en la actividad 3, más `Restore` y `Version` | `src/Domain/Deliveries/Delivery.cs` | `DeliveryTests`, `DeliveryRestoreTests` |
| `AssignCourierUseCase` con los 6 pasos | Sin cambios | `src/Application/UseCases/AssignCourierUseCase.cs` | `AssignCourierUseCaseTests` |
| Puerto `IDeliveryRepository` | Adaptador `SqliteDeliveryRepository` | `src/Infrastructure/Persistence/` | `SqliteDeliveryRepositoryTests` |
| Puerto `ICourierAvailabilityProvider` | Adaptador `SqliteCourierAvailabilityProvider` | `src/Infrastructure/Couriers/` | `SqliteCourierAvailabilityProviderTests` |
| Puerto `IDomainEventPublisher` | Adaptador `ResilientNotificationEventPublisher` | `src/Infrastructure/Notifications/` | `ResilientNotificationEventPublisherTests` |
| Puerto `IClock` | `SystemClock` | `src/Infrastructure/Time/` | `FixedClock` en las pruebas |
| Contratos `AssignCourierCommand` y `AssignCourierResult` | Sin cambios | `src/Application/Contracts/` | `DeliveryApiTests` |
| Errores de aplicación y de dominio | Traducidos a HTTP en un solo lugar | `src/Api/ErrorMapping.cs` | `DeliveryApiTests` (404, 409, 422) |
| Reglas de dependencias (ADR-002) | Pruebas de arquitectura | `tests/.../Architecture/` | `DependencyRulesTests`, `FitnessFunctionTests` |
| Riesgo del ADR-004: concurrencia | Control de versión optimista | `SqliteDeliveryRepository` | `Dos_asignaciones_concurrentes...` |
| Riesgo del ADR-004: falla al publicar | Reintentos y cola de mensajes fallidos | `ResilientNotificationEventPublisher` | `Si_el_servicio_de_avisos_cae...` |

## Escenarios de la Actividad 3 y dónde se comprueban ahora
| Escenario | Pruebas del dominio y la aplicación | Prueba de integración por HTTP |
|---|---|---|
| E-01 Asignación exitosa | `AssignCourier_en_entrega_pendiente...` | `Flujo_completo_crea_asigna_y_consulta...` |
| E-02 Ya tiene repartidor | `AssignCourier_dos_veces...` | `Asignar_una_entrega_que_ya_tiene_repartidor_devuelve_409` |
| E-03 Repartidor fuera de línea | `AssignCourier_con_repartidor_fuera_de_linea...` | `Un_repartidor_fuera_de_linea_devuelve_422` |
| E-04 Ruta saturada | `AssignCourier_con_ruta_saturada...` | `Limite_una_ruta_saturada_devuelve_422...` |
| E-05 y E-06 No existen | `Entrega_inexistente...`, `Repartidor_inexistente...` | `Entrega_o_repartidor_inexistentes_devuelven_404` |
| E-08 Se publica el evento | `AssignCourier_registra_el_evento...` | `Flujo_completo...` (el aviso se envía una vez) |

## Ajustes al diseño de la Actividad 3 (decisiones de implementación)
Tuve que cambiar o agregar cosas que el diseño no cubría. Las dejo explícitas:

| Ajuste | Por qué | Documentado en |
|---|---|---|
| `Delivery.Restore` y `Delivery.Version` en el dominio | Para reconstruir una entrega desde la base de datos sin saltarme las invariantes, y para detectar modificaciones concurrentes | [ADR-006](adr/ADR-006-persistencia-migraciones-y-concurrencia.md) |
| Dos casos de uso nuevos: `CreateDeliveryUseCase` y `GetDeliveryUseCase` | Sin crear ni consultar entregas, el flujo no se puede ejecutar ni demostrar | [ADR-005](adr/ADR-005-api-rest-y-vertical-slice.md) |
| Puerto `IDeadLetterStore` y caso de uso `ListDeadLettersUseCase` | El ADR-004 dejó pendiente qué hacer si falla la publicación | [ADR-007](adr/ADR-007-notificaciones-resilientes-y-cola-de-fallidos.md) |
| La dirección no se devuelve en la consulta de entregas | Minimizar la exposición de datos personales | [ADR-008](adr/ADR-008-seguridad-observabilidad-y-verificacion.md) |
| Clave de despachador en el encabezado `X-Api-Key` | Versión simplificada del contexto Identidad y accesos | [ADR-008](adr/ADR-008-seguridad-observabilidad-y-verificacion.md) |

## Estructura final del repositorio
```
src/
  Domain/            reglas del negocio (sin dependencias)
  Application/       casos de uso, contratos y puertos
  Infrastructure/    SQLite, migraciones, notificaciones, telemetría
  Api/               entrada HTTP (Minimal API), seguridad y composición
tests/PixelRoute.Tests/   55 pruebas: dominio, aplicación, infraestructura, API y arquitectura
docs/                actividades 2, 3 y 4
.github/workflows/   integración continua
scripts/demo.ps1     demostración del flujo
requests.http        peticiones de ejemplo
```
