# ADR-001: Estructura del sistema como monolito modular por dominios con puertos y adaptadores

| Campo | Valor |
|---|---|
| **Estado** | Aceptada |
| **Fecha** | 29 de octubre de 2026 |
| **Proyecto** | PixelRoute (PixelPack) |
| **Decisor** | Andrés Barrera Claros |
| **Relacionada con** | [Contexto y diagnóstico](00-contexto-y-diagnostico.md) |

## 1. Contexto
PixelPack necesita una plataforma centralizada para cumplir su promesa de entrega en 3 horas desde 6 micro-bodegas, con repartidores independientes. Hay que decidir **cómo se estructura el sistema** antes de elegir tecnologías. Se espera crecimiento moderado con picos fuertes en lanzamientos (S1 y S2), el equipo es pequeño (S6) y el presupuesto limitado (RES-04).

## 2. Fuerzas que influyen
- **Consistencia de inventario y pedidos** (RNF-01): no se puede vender lo que no existe en una micro-bodega ni despachar dos veces.
- **Asignación de repartidores** (RF-10): la regla que más va a cambiar con el negocio.
- **Trazabilidad** (RNF-08): historial único y confiable de cada pedido.
- **Evolución y testabilidad** (RNF-09 y RNF-10): las reglas cambian y hay que poder probarlas sin infraestructura real.
- **Picos de demanda** (RNF-03): los lanzamientos concentran las ventas.
- **Aislamiento de fallas externas** (RNF-05): cuatro sistemas externos pueden fallar.
- **Equipo pequeño y presupuesto limitado** (RES-04).
- **Falta de evidencia de crecimiento extremo**: todavía no sabemos si hará falta escalar módulos por separado.

## 3. Opciones consideradas
Detalle y puntajes en la [comparación de alternativas](05-comparacion-de-alternativas.md).

| Opción | Base | Crecimiento alto | Presupuesto austero |
|---|---|---|---|
| A. Monolito en capas tradicional | 3,50 | 3,05 | 4,15 |
| **B. Monolito modular por dominios con puertos y adaptadores** | **4,10** | **3,95** | 3,95 |
| C. Microservicios por dominio | 2,70 | 3,30 | 2,15 |

## 4. Decisión
**Se adopta la opción B: un monolito modular organizado por dominios, con puertos y adaptadores dentro de cada módulo.**

1. El sistema se construye y despliega como **una sola unidad**.
2. Se divide en módulos: **Pedidos, Inventario, Despacho y flota, Incidencias y soporte e Identidad y accesos** ([documento 04](04-dominios-y-limites.md)).
3. Cada módulo es **dueño de sus datos** (un esquema por módulo); ningún módulo lee las tablas de otro.
4. Los módulos se comunican **solo por contratos y eventos de dominio internos**.
5. Dentro de cada módulo, los **casos de uso dependen de puertos** (interfaces) y la infraestructura se implementa en **adaptadores** ([documento 08](08-ejemplo-de-contratos.md)).
6. Los sistemas externos se conectan mediante adaptadores sin reglas de negocio.
7. No se introduce un broker de mensajes externo ni microservicios en esta etapa.

La decisión es **estructural**: no elige lenguajes, frameworks ni bases de datos (RES-06).

## 5. Justificación
- **Consistencia (QS-03, QS-04):** pedidos e inventario viven en la misma unidad, así que reservar stock y confirmar el pedido puede hacerse en una sola transacción.
- **Evolución (QS-12, QS-13):** los límites por dominio y los adaptadores hacen que cambiar la regla de asignación o agregar un canal afecte pocas partes.
- **Testabilidad (QS-15):** al depender de puertos, la lógica se prueba con implementaciones falsas, sin base de datos ni servicios externos.
- **Resiliencia (QS-08):** los adaptadores aíslan al núcleo de las fallas de los sistemas externos.
- **Costo y riesgo (RES-04):** menos infraestructura, menos puntos de falla y despliegue simple, viable para un equipo pequeño.
- **Escalabilidad (QS-02):** 600 pedidos por hora es una carga moderada que se cubre replicando el sistema completo.
- **Reversibilidad:** como cada módulo tiene datos y contratos separados, extraerlo como servicio más adelante es posible sin rehacer todo.

## 6. Consecuencias

### Positivas
- Construcción y operación más baratas que con microservicios.
- Transacciones simples entre pedidos e inventario.
- Lógica de negocio fácil de probar y de cambiar.
- Un solo lugar para monitorear y auditar.
- Camino claro para evolucionar a servicios si hace falta.

### Trade-offs aceptados
| Trade-off | Qué aceptamos | Cómo lo mitigamos |
|---|---|---|
| **Más trabajo inicial que las capas** | Definir módulos, contratos y puertos antes de construir. | Empezar por los casos de uso críticos (crear pedido, reservar stock, asignar repartidor). |
| **Escalado en bloque** | En un pico se replica todo el sistema aunque solo un módulo esté saturado. | Pruebas de carga periódicas y criterios de reevaluación (sección 8). |
| **Riesgo de acoplamiento** | Sin disciplina, los módulos se enredan. | Reglas de acoplamiento, pruebas de arquitectura y revisión de código. |
| **Despliegue conjunto** | Un cambio pequeño obliga a desplegar todo. | Pruebas automáticas y despliegues pequeños y frecuentes. |
| **Falla compartida** | Un error grave en un módulo podría afectar al sistema entero. | Adaptadores con reintentos y colas; manejo de errores por módulo. |
| **Tecnología homogénea** | Todos los módulos usan la misma tecnología. | Aceptable en esta etapa; se reevalúa si un módulo lo requiere. |

## 7. Riesgos de la decisión
| Riesgo | Prob. | Impacto | Mitigación |
|---|---|---|---|
| El monolito se vuelve una "bola de lodo" | M | A | Reglas de acoplamiento, pruebas de arquitectura, revisiones periódicas. |
| El crecimiento supera lo que aguanta el monolito | B | A | Criterios de reevaluación y pruebas de carga. |
| Los supuestos de volumen (S1, S2) están equivocados | M | M | Confirmar con el negocio y repetir las pruebas con datos reales. |
| Una falla externa afecta al núcleo | M | A | Adaptadores aislados, colas y reintentos (QS-08). |
| Los puertos se diseñan mal y obligan a cambios en cascada | M | M | Mantener los puertos pequeños y centrados en lo que necesita el caso de uso. |

## 8. Criterios para reevaluar la decisión
Se revisa este ADR, y se considera extraer un módulo como servicio, si se cumple alguna condición de forma sostenida:
1. Un módulo necesita **más de 5 veces** la capacidad de los demás para cumplir QS-02.
2. La **disponibilidad** cae por debajo de QS-07 por fallas atribuibles a un solo módulo.
3. Varios equipos trabajan en módulos distintos y la **coordinación de despliegues** frena las entregas.
4. Las **violaciones de límites** detectadas por las pruebas de arquitectura son recurrentes.
5. Un módulo necesita una **tecnología o ciclo de despliegue** distinto al resto.

## 9. Cómo se verifica
Los [criterios verificables](07-criterios-verificables.md) definen las pruebas y los umbrales.

## 10. Decisiones pendientes (próximos ADR)
- Elección de tecnologías y base de datos.
- Estrategia de autenticación y manejo de datos de menores.
- Contratos detallados de las integraciones externas.
