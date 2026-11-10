# 04. Pruebas, resultados, riesgos residuales y próximos pasos

## Suite de pruebas
**55 pruebas** automáticas en `tests/PixelRoute.Tests`:

| Capa | Archivo | Pruebas | Qué comprueba |
|---|---|---|---|
| Dominio | `DeliveryTests` | 10 | Invariantes INV-01 a INV-08 (incluye dirección vacía) |
| Dominio | `DeliveryRestoreTests` | 3 | Reconstrucción coherente desde la base de datos (INV-03) |
| Aplicación | `AssignCourierUseCaseTests` | 5 | Caso de uso con puertos falsos: éxito, no existe, ya asignada, ruta saturada |
| Aplicación | `CreateAndGetDeliveryUseCaseTests` | 4 | Crear y consultar entregas |
| Infraestructura | `SqlMigrationRunnerTests` | 4 | Migraciones reproducibles y restricciones del esquema |
| Infraestructura | `SqliteDeliveryRepositoryTests` | 5 | Guardar, leer, **conflicto concurrente** y texto malicioso |
| Infraestructura | `SqliteCourierAvailabilityProviderTests` | 2 | Conteo de entregas activas y repartidor inexistente |
| Infraestructura | `ResilientNotificationEventPublisherTests` | 2 | Reintentos y cola de mensajes fallidos |
| Integración | `AssignCourierFlowTests` | 1 | Flujo con adaptadores en memoria |
| Integración | `DeliveryApiTests` | 11 | **Flujo completo por HTTP con SQLite real**, errores 401, 404, 409, 422 y 500, avisos caídos y registros sin datos sensibles |
| Arquitectura | `DependencyRulesTests` | 3 | Dirección de las dependencias entre proyectos |
| Arquitectura | `FitnessFunctionTests` | 5 | Dominio y aplicación puros, infraestructura sin ASP.NET, casos de uso solo con interfaces, entidad sin setters públicos |
| **Total** | | **55** | |

**Casos de error y de límite comprobados** (la actividad exige al menos uno): ruta saturada (422), entrega ya asignada (409), conflicto concurrente de versión, repartidor fuera de línea, servicio de avisos caído, texto malicioso, error inesperado y falta de clave.

## Cómo ejecutarlas
```powershell
dotnet test
```
Para ver el nombre de cada prueba:
```powershell
dotnet test --logger "console;verbosity=detailed"
```
La integración continua las ejecuta en cada `push` y en cada Pull Request (pestaña **Actions** de GitHub).

## Evidencia de ejecución (completar antes de entregar)
| Dato | Valor |
|---|---|
| Fecha de ejecución | (escribir) |
| Resultado local de `dotnet test` | (pegar: "Pruebas totales: 55, Correcto: 55, Con errores: 0") |
| Enlace a la ejecución de GitHub Actions | (pegar el enlace de la pestaña Actions) |
| Captura de pantalla | (agregar en `docs/actividad-4/evidencia/`) |

## Comprobación automatizada de calidad y arquitectura
- **Dónde:** `FitnessFunctionTests` y `DependencyRulesTests`, ejecutadas con todas las pruebas y en GitHub Actions. Si alguien agrega una referencia prohibida, el CI falla y el cambio no debería integrarse.
- **Qué protege:**

| Regla | Prueba |
|---|---|
| Domain no referencia a Application ni Infrastructure | `Domain_no_depende_de_Application_ni_de_Infrastructure` |
| Application no referencia a Infrastructure | `Application_depende_de_Domain_pero_no_de_Infrastructure` |
| Domain y Application no usan paquetes de Microsoft | `El_dominio_no_depende...`, `La_aplicacion_no_depende...` |
| Infrastructure no usa ASP.NET Core | `La_infraestructura_no_depende_de_ASP_NET_Core` |
| Los casos de uso reciben solo interfaces | `Los_casos_de_uso_solo_reciben_interfaces_por_constructor` |
| `Delivery` no se puede modificar desde fuera | `La_entidad_Delivery_no_expone_setters_publicos` |

## Métricas de arquitectura (video 13)
| Métrica | Cómo se mide | Objetivo | Valor medido |
|---|---|---|---|
| Violaciones de dependencias entre capas | `DependencyRulesTests` y `FitnessFunctionTests` | 0 | (anotar tras ejecutar) |
| Paquetes de infraestructura en Domain y Application | `FitnessFunctionTests` | 0 | (anotar tras ejecutar) |
| Cantidad de pruebas | `dotnet test` | ≥ 50 | (anotar) |
| Pruebas que fallan | `dotnet test` | 0 | (anotar) |
| Duración de la suite | Resumen de `dotnet test` | < 30 s | (anotar) |
| Paquetes con vulnerabilidades conocidas | `dotnet list package --vulnerable --include-transitive` | 0 | (anotar) |
| Cobertura de invariantes (INV-01 a INV-08) | Tabla de trazabilidad del [documento 03 de la Actividad 3](../actividad-3/03-invariantes-y-escenarios.md) | 8 de 8 | 8 de 8 |

## Resultados
**Qué funciona:**
- El flujo completo por HTTP: crear, asignar y consultar, con persistencia real y migraciones reproducibles.
- Las invariantes se cumplen en cada capa donde corresponde: dominio, esquema de la base de datos y control de versión.
- Las fallas del servicio de avisos no afectan a la asignación y quedan en una cola revisable.
- No aparecen direcciones ni claves en los registros.
- La arquitectura se verifica sola en cada ejecución.

## Riesgos residuales
| ID | Riesgo residual | Por qué quedó así |
|---|---|---|
| RO-02 | Un corte exacto entre guardar y publicar pierde el aviso | Resolverlo bien exige un outbox transaccional |
| RO-11 | QS-02 (600 pedidos por hora) no está validado; SQLite escribe de a uno | No hay prueba de carga ni un motor pensado para concurrencia |
| RO-12 | El cupo de un repartidor puede superarse con asignaciones simultáneas de entregas distintas | La regla involucra a dos agregados y requiere una operación atómica |
| RO-14 | Sin límite de solicitudes | Queda para una capa de protección |
| Seguridad | Una sola clave compartida, sin roles | Falta implementar el contexto Identidad y accesos |
| Integración | El servicio de notificaciones es simulado | No hay un proveedor real disponible en este entorno |
| Estados | Solo se implementaron las transiciones del flujo (Pendiente, Asignada, Cancelada) | Las demás (En ruta, Entregada, Fallida) están diseñadas pero fuera del alcance |

## Próximos pasos (en orden de prioridad)
1. **Outbox transaccional** y un proceso que despache los eventos con reintentos (cierra RO-02 y mejora RO-01).
2. **Asignación atómica del cupo** del repartidor (cierra RO-12).
3. **PostgreSQL** y una **prueba de carga** para validar QS-02 (cierra RO-11).
4. **JWT y roles**, y limitación de solicitudes por clave (RO-05 y RO-14).
5. Exportar métricas y trazas con **OpenTelemetry**.
6. Implementar las demás transiciones de `Delivery` (En ruta, Entregada, Fallida) y la reasignación automática.
7. Activar las reglas de protección de la rama `main` en GitHub (rulesets) y análisis de código con CodeQL.

## Checklist de la actividad y dónde está cada evidencia
| Requisito del PDF | Evidencia |
|---|---|
| Flujo funcional implementado | `src/` y `scripts/demo.ps1` |
| Arquitectura con capas o límites claros | [Architecture.md](../../Architecture.md), ADR-002 |
| Persistencia o integración externa aislada | `src/Infrastructure` detrás de puertos; ADR-006 y ADR-007 |
| Manejo de errores y fallos | `ErrorMapping`, `ResilientNotificationEventPublisher`; ADR-007 |
| Migraciones reproducibles | `src/Infrastructure/Persistence/Migrations`, `SqlMigrationRunnerTests` |
| README con instrucciones de ejecución | [README.md](../../README.md) |
| Pruebas unitarias de reglas críticas | `DeliveryTests`, `DeliveryRestoreTests` |
| Pruebas de integración y evidencia | `DeliveryApiTests`, `AssignCourierFlowTests`, ejecuciones de GitHub Actions |
| Caso de error o límite | `Limite_una_ruta_saturada...`, `Dos_asignaciones_concurrentes...` |
| Riesgos operativos y de seguridad con mitigaciones | [Documento 02](02-riesgos-seguridad-y-operacion.md) |
| Observabilidad mínima | [Documento 03](03-observabilidad.md) |
| Comprobación automatizada de arquitectura | `FitnessFunctionTests`, `.github/workflows/ci.yml` |
| Riesgos residuales y próximos pasos | Este documento |
| Decisiones vinculadas a la Actividad 3 | [Documento 00](00-plan-y-trazabilidad.md) |
| Expediente arquitectónico actualizado | [Architecture.md](../../Architecture.md) y diagramas C4 |
| Video de demostración y defensa | Enlace en el README |
