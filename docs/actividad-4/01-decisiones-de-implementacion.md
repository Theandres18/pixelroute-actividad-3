# 01. Decisiones de implementación y qué patrones aplican

La actividad pide seleccionar los videos de implementación según las decisiones de las actividades 2 y 3, y **justificar cuáles aplican y cuáles no**. Esta es mi selección.

## Decisiones tomadas (ADR)
| ADR | Decisión |
|---|---|
| [ADR-005](adr/ADR-005-api-rest-y-vertical-slice.md) | Entrada REST con Minimal API y un vertical slice ejecutable |
| [ADR-006](adr/ADR-006-persistencia-migraciones-y-concurrencia.md) | SQLite, migraciones SQL versionadas y control de concurrencia optimista |
| [ADR-007](adr/ADR-007-notificaciones-resilientes-y-cola-de-fallidos.md) | Adaptador de notificaciones con reintentos y cola de mensajes fallidos |
| [ADR-008](adr/ADR-008-seguridad-observabilidad-y-verificacion.md) | Seguridad mínima, observabilidad y verificación automática |

Las decisiones anteriores siguen vigentes: [ADR-001](../actividad-2/06-adr-001-monolito-modular.md) (monolito modular), [ADR-002](../actividad-3/adr/ADR-002-tres-capas-y-direccion-de-dependencias.md) (tres capas), [ADR-003](../actividad-3/adr/ADR-003-agregado-delivery-e-invariantes.md) (agregado `Delivery`) y [ADR-004](../actividad-3/adr/ADR-004-puertos-eventos-y-consistencia.md) (puertos y eventos).

## Para construir e integrar
| Video | ¿Aplica? | Justificación |
|---|---|---|
| **3. Monorepos con Pants** | No | Es un solo repositorio con una sola solución .NET de cuatro proyectos. Una herramienta de construcción para monorepos agregaría complejidad sin beneficio. Sí mantuve la idea de organizar todo en `src`, `tests` y `docs`. |
| **4. Trunk Based Development con rulesets** | Parcialmente | Trabajo con una rama corta (`feat/actividad-4`), un Pull Request y la integración continua como puerta de entrada a `main`. Las reglas de protección de rama (rulesets) son opcionales y se pueden activar en GitHub; no cambian el código. |
| **14. Strangler Fig** | No | No hay un sistema legado que migrar. Es un sistema nuevo. |
| **15. Migraciones con Flyway** | Sí, en espíritu | Apliqué la idea de migraciones SQL versionadas, ordenadas y registradas en una tabla. No usé Flyway porque es una herramienta de Java; con .NET escribí un ejecutor pequeño (`SqlMigrationRunner`). Limitación: no genera sumas de verificación ni migraciones de reversa. |
| **18. Infraestructura como código** | Parcialmente | El flujo de integración continua está como código (`.github/workflows/ci.yml`). No hay infraestructura que aprovisionar: la base es un archivo y la API corre como un proceso. |
| **16. API Gateway** | No | Hay un solo servicio. Un gateway agregaría un salto de red sin aportar. Las responsabilidades de la entrada (autenticación, validación, correlación y errores) las cumple la propia capa Api, y podrían moverse a un gateway si el sistema se divide en servicios. |
| **19. Mensajes vs eventos** | Sí | `DeliveryAssigned` es un **evento** (un hecho que ocurrió). El aviso al cliente es un **mensaje** (una orden de enviar algo). El adaptador traduce de uno al otro, y el dominio no sabe nada de avisos. |
| **20. Productor-consumidor, fan-in y fan-out** | No | No hay una cola con varios consumidores. El publicador procesa los eventos de forma directa. Sería necesario con picos de volumen. |
| **21. Dead Letter Queue** | Sí | Si el aviso falla tras 3 intentos, se guarda en la tabla `dead_letter_messages` y se puede revisar en `/api/v1/admin/dead-letters`. |
| **22. Comparing Consumers** | No | Requiere varios consumidores de un mismo flujo en tiempo real, que no existen aquí. |
| **23. Process Manager** | No | El flujo implementado es una sola operación. Sería útil para coordinar el recorrido completo (pedido → pago → despacho → entrega), que está fuera del alcance. |
| **24. Durable State vs Event Sourcing** | Estado durable | Se guarda el estado actual de cada entrega, con su versión. Event Sourcing daría un historial completo, pero agrega complejidad (reconstrucción, versionado de eventos) que no se justifica para este flujo. |
| **25. Máquinas de estado en el front-end** | No | No hay interfaz. Las transiciones de estado viven en la entidad `Delivery`, como decidí en el ADR-003. |

## Para probar, operar y defender
| Video | ¿Aplica? | Cómo lo apliqué |
|---|---|---|
| **8. Agentes de IA que revisan código** | Como apoyo | Puede ayudar a revisar un Pull Request, pero no sustituye las pruebas ni mi criterio. Las pruebas automáticas y la integración continua son la comprobación principal. |
| **11. Pre-mortem y cinco porqués** | Sí | Documento [02](02-riesgos-seguridad-y-operacion.md): imaginé que el flujo falló y busqué las causas. |
| **12. El pre-mortem guía los tests de arquitectura** | Sí | Cada riesgo importante tiene una prueba que lo comprueba (columna "Evidencia" del documento 02). |
| **13. Métricas cuantitativas** | Sí | Tabla de métricas de arquitectura en el [documento 04](04-pruebas-y-resultados.md). |
| **26. SAST, DAST y pentesting** | Parcialmente | Consultas parametrizadas con prueba de texto malicioso, comparación de claves en tiempo constante, revisión de paquetes vulnerables (`dotnet list package --vulnerable`) y errores sin detalles internos. No hice un análisis dinámico ni un pentest. |
| **27. Fitness Functions** | Sí | `FitnessFunctionTests` y `DependencyRulesTests`, ejecutados en cada compilación y en la integración continua. |
| **28. Observabilidad y caos** | Sí | Señales en el [documento 03](03-observabilidad.md) y un experimento de caos controlado: apagar el servicio de avisos y comprobar que la asignación sigue funcionando. No usé el SDK de OpenTelemetry; dejé las métricas y trazas en la API estándar de .NET, lista para exportarse. |
| **29 y 30. Criterio y defensa final** | Sí | Guion de la sustentación en el [documento 05](05-guion-video.md). |
