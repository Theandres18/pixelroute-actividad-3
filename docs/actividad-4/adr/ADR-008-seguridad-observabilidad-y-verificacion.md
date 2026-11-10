# ADR-008: Seguridad mínima, observabilidad y verificación automática

| Campo | Valor |
|---|---|
| **Estado** | Aceptada |
| **Fecha** | 29 de octubre de 2026 |
| **Decisor** | Andrés Barrera Claros |

## Contexto
El flujo maneja datos personales (direcciones) y operaciones sensibles (asignar repartidores). Hay que decidir cómo se protege la entrada, qué señales permiten diagnosticar fallas **sin exponer datos sensibles**, y cómo se comprueba la arquitectura de forma automática.

## Opciones consideradas

### Seguridad de la entrada
| Opción | Resultado |
|---|---|
| A. Sin autenticación | Descartada: cualquiera podría asignar repartidores. |
| **B. Clave de despachador en `X-Api-Key`, comparada en tiempo constante, y que falla cerrada si no está configurada** | **Elegida** |
| C. JWT con un proveedor de identidad | Descartada por ahora: requiere implementar el contexto Identidad y accesos completo. Es el siguiente paso. |

### Observabilidad
| Opción | Resultado |
|---|---|
| A. Solo `Console.WriteLine` | Descartada: sin estructura ni correlación. |
| **B. Registros estructurados, identificador de correlación, métricas y trazas con la API estándar de .NET y `/health`** | **Elegida** |
| C. SDK de OpenTelemetry y un recolector | Descartada por ahora: dependencias e infraestructura adicionales. Las señales ya están definidas con la API estándar, así que se pueden exportar después. |

### Verificación de arquitectura
| Opción | Resultado |
|---|---|
| A. Revisión manual | Descartada: no se repite sola. |
| **B. Funciones de aptitud como pruebas automáticas, ejecutadas también en la integración continua** | **Elegida** |

## Decisión
- **Seguridad:** `ApiKeyEndpointFilter` protege `/api/v1`. En `appsettings.json` la clave está vacía (todo se rechaza); la clave de desarrollo solo existe en `appsettings.Development.json`. Entradas validadas, consultas SQL parametrizadas, errores inesperados sin detalles internos, y la consulta de entregas no devuelve la dirección.
- **Observabilidad:** `X-Correlation-Id` (se acepta solo si es seguro: letras, números, guiones y máximo 64 caracteres) agregado a todos los registros; métricas con `Meter` ("PixelRoute"); trazas con `ActivitySource`; `GET /health`. **Nunca se registran** direcciones, claves ni contenido de mensajes: solo identificadores, resultados y tipos de error.
- **Verificación:** `FitnessFunctionTests` y `DependencyRulesTests` ejecutadas en cada compilación y en GitHub Actions.

## Justificación
- Reduce los riesgos más probables con el menor costo y sin inventar un sistema de identidad completo.
- Los registros permiten investigar un fallo (con el identificador de correlación) sin filtrar datos personales, y hay una prueba que lo comprueba.
- Las reglas de arquitectura no dependen de que alguien se acuerde de revisarlas.

## Trade-offs
| Trade-off | Qué aceptamos | Cómo lo manejamos |
|---|---|---|
| Una sola clave compartida | No distingue entre usuarios ni roles; no se puede rotar por usuario. | Riesgo residual; próximo paso: JWT y roles. |
| Sin límite de solicitudes | Una clave filtrada permitiría abusar de la API. | Riesgo residual; próximo paso: limitar por clave. |
| Métricas y trazas sin exportador | Hay que usar `dotnet-counters` para verlas. | Suficiente para esta etapa. |
| Las funciones de aptitud revisan referencias de ensamblados | Detectan dependencias, no cada mala práctica. | Se complementan con revisión y con pruebas funcionales. |

## Verificación
`Sin_la_clave_de_despachador...`, `Los_registros_no_contienen_la_direccion_ni_la_clave`, `Un_error_inesperado_devuelve_500_generico...`, `El_identificador_de_correlacion...` y `FitnessFunctionTests`.
