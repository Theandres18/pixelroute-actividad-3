# ADR-005: Entrada REST con Minimal API y un vertical slice ejecutable

| Campo | Valor |
|---|---|
| **Estado** | Aceptada |
| **Fecha** | 29 de octubre de 2026 |
| **Decisor** | Andrés Barrera Claros |
| **Relacionada con** | [ADR-002](../../actividad-3/adr/ADR-002-tres-capas-y-direccion-de-dependencias.md) |

## Contexto
La Actividad 3 dejó diseñado `AssignCourierUseCase` sin una forma de invocarlo. Hay que elegir la **entrada del sistema** y qué se implementa para que el flujo se pueda ejecutar y demostrar de punta a punta.

## Opciones consideradas
| Opción | Resultado |
|---|---|
| **A. API REST con ASP.NET Core Minimal API** | **Elegida** |
| B. API REST con controladores MVC | Descartada: más ceremonia (clases, atributos) para cuatro endpoints. |
| C. Aplicación de consola (CLI) | Descartada: sirve para una demo local, pero no representa la entrada del sistema ni se integra con canales de venta. |
| D. Entrada por eventos o cola | Descartada: no hay un productor de eventos disponible. |
| E. gRPC | Descartada: el cliente natural (canales web y Discord) usa HTTP y JSON. |

## Decisión
- Un proyecto `PixelRoute.Api` con **Minimal API**. Cada endpoint recibe un DTO, **valida el formato**, invoca un caso de uso y devuelve la respuesta. No contiene reglas del negocio ni acceso a datos.
- Se implementa un vertical slice con cuatro endpoints: crear entrega, consultar entrega, **asignar repartidor** y listar mensajes fallidos, además de `/health`.
- Los errores se traducen a HTTP en un solo lugar (`ErrorMapping`): 404 si algo no existe, 409 si hay conflicto de estado, 422 si se incumple una regla (repartidor fuera de línea o ruta saturada) y 400 si el dato es inválido.
- La **raíz de composición** (`Program.cs`) es el único lugar donde se eligen los adaptadores concretos.

## Justificación
- Conecta la entrada con el caso de uso mediante los **contratos** definidos en la Actividad 3 (`AssignCourierCommand` y `AssignCourierResult`).
- Mantiene el dominio como pieza central: la API solo depende de Application e Infrastructure, y el dominio no sabe que existe HTTP.
- Crear y consultar entregas es necesario para poder demostrar el flujo sin datos precargados a mano.

## Trade-offs
| Trade-off | Qué aceptamos | Cómo lo manejamos |
|---|---|---|
| Más casos de uso de los diseñados | Se agregan crear y consultar. | Son pequeños y están cubiertos por pruebas. |
| Sin versionado completo de la API | Solo el prefijo `/api/v1`. | Suficiente para esta etapa. |
| La validación de formato está en la entrada y la de negocio en el dominio | Dos niveles de validación. | Es lo buscado: el formato falla rápido con 400; las reglas siempre las protege el dominio. |

## Verificación
`DeliveryApiTests` (flujo completo y códigos de error) y `FitnessFunctionTests` (la infraestructura no depende de ASP.NET Core).
