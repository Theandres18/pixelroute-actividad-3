# ADR-002: Tres capas con las dependencias apuntando hacia el dominio

| Campo | Valor |
|---|---|
| **Estado** | Aceptada |
| **Fecha** | 29 de octubre de 2026 |
| **Decisor** | Andrés Barrera Claros |
| **Relacionada con** | [ADR-001](../../actividad-2/06-adr-001-monolito-modular.md) (monolito modular con puertos y adaptadores) |

## Contexto
El ADR-001 decidió un monolito modular con puertos y adaptadores. Para el flujo de asignar una entrega hay que concretar **cuántas capas** se usan y **hacia dónde apuntan las dependencias**. El consejo de la actividad es que la lógica de negocio pueda cambiar sin depender de una base de datos, una API externa o un framework concreto (RNF-10, QS-15).

## Opciones consideradas
| Opción | Descripción | Resultado |
|---|---|---|
| **A. Tres capas: Domain, Application, Infrastructure** | El dominio no depende de nada; la aplicación depende del dominio; la infraestructura implementa los puertos de la aplicación. | **Elegida** |
| B. Dos capas: negocio mezclado con casos de uso, e infraestructura | Menos proyectos, pero las reglas y la coordinación quedan juntas. | Descartada |
| C. Capas tradicionales: presentación, lógica de negocio y acceso a datos | Las dependencias apuntan hacia abajo, hacia los datos. | Descartada |
| D. Cuatro capas, con una capa de presentación desde ya | Agrega una API o pantalla que esta actividad no pide. | Descartada por ahora; se agrega en la Actividad 4 |

## Decisión
Se usan **tres capas** como proyectos separados: `PixelRoute.Domain`, `PixelRoute.Application` y `PixelRoute.Infrastructure`. Las dependencias de proyecto son **Infrastructure → Application → Domain**, y se comprueban con pruebas de arquitectura.

## Justificación
- **Dominio protegido:** las reglas críticas (invariantes) viven en un proyecto que no referencia nada de afuera.
- **Casos de uso sin infraestructura:** al depender de puertos, se prueban con implementaciones falsas (QS-15), sin base de datos ni red.
- **Cambios baratos:** cambiar de base de datos o de proveedor de notificaciones solo toca la infraestructura.
- **Coherencia con la Actividad 2:** es lo que el ADR-001 llamó puertos y adaptadores dentro de cada módulo.
- **Dos capas (B)** esconden la diferencia entre "qué regla se protege" y "cómo se coordina el flujo", y vuelven más difícil probar cada cosa por separado.
- **Capas tradicionales (C)** dejan la lógica dependiendo de los datos, que es justo lo que se quiere evitar.

## Consecuencias y trade-offs
| Trade-off | Qué aceptamos | Cómo lo manejamos |
|---|---|---|
| Más proyectos y más código base que un enfoque simple | Para un flujo pequeño parece mucha estructura. | Es la base de todo el sistema; el costo se amortiza con los demás flujos. |
| Mapeos entre capas | Los contratos (`AssignCourierCommand`) no son las entidades. | Contratos pequeños y con tipos simples. |
| Disciplina necesaria | Sin cuidado, alguien puede agregar una referencia que rompa la regla. | Pruebas de arquitectura (`DependencyRulesTests`) y revisión de código. |

## Cómo se verifica
- `DependencyRulesTests` (CV-01): Domain no referencia Application ni Infrastructure; Application no referencia Infrastructure.
- Revisión de que los casos de uso no importen clases de base de datos, HTTP ni mensajería (CV-04).
