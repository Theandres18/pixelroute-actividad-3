# 05. Comparación de alternativas estructurales

Comparé tres alternativas. El enfoque basado en eventos no lo traté como alternativa aparte, sino como técnica complementaria dentro de la elegida (ver [documento 04](04-dominios-y-limites.md)).

## Alternativas

### Opción A: Monolito en capas tradicional
Un solo sistema organizado por capas técnicas (presentación, lógica, acceso a datos), sin separar por dominios.
- **Ventajas:** es lo más simple y rápido de arrancar; muy conocido; barato.
- **Desventajas:** pedidos, inventario y despacho terminan mezclados; la lógica queda atada a la base de datos y a los servicios externos, así que probarla requiere infraestructura real; cambiar una regla toca varias capas.

### Opción B: Monolito modular por dominios con puertos y adaptadores
Un solo sistema que se despliega como una unidad, dividido en módulos por dominio (Pedidos, Inventario, Despacho y flota, Incidencias y soporte, Identidad y accesos). Dentro de cada módulo, los casos de uso dependen de puertos y los adaptadores implementan la infraestructura.
- **Ventajas:** transacciones simples entre pedidos e inventario; costo y operación bajos; límites claros que permiten separar un módulo después; la lógica se prueba sin infraestructura real.
- **Desventajas:** requiere disciplina para no acoplar los módulos; todo se despliega junto; en un pico se escala todo el sistema.

### Opción C: Arquitectura distribuida por dominios (microservicios)
Cada dominio es un servicio independiente con su propia base de datos, que se despliega y escala por separado.
- **Ventajas:** escala cada parte por separado; permite equipos independientes; aísla mejor las fallas.
- **Desventajas:** mucha más complejidad operativa; mantener el inventario sin sobreventas entre servicios es difícil; necesita más infraestructura y personas; con un equipo pequeño se sobrediseña fácil.

## Criterios y pesos
Los pesos reflejan los requisitos arquitectónicamente significativos del [documento 02](02-matriz-de-priorizacion.md) y la restricción RES-04. En todos los criterios, **5 es lo mejor** (5 en "complejidad" significa menos complejo).

| Criterio | Peso | A: Capas | B: Monolito modular | C: Microservicios | Por qué |
|---|---|---|---|---|---|
| Complejidad técnica y operativa | 15 % | 5 | 4 | 1 | C exige orquestar muchos servicios; B agrega solo la disciplina de módulos y puertos. |
| Costo de construcción y operación | 10 % | 5 | 4 | 2 | C necesita más infraestructura y personas. |
| Rapidez de implementación | 10 % | 5 | 3 | 2 | B pide definir módulos y puertos antes de construir. |
| Facilidad de evolución | 15 % | 2 | 4 | 5 | A se enreda al crecer; C es el más flexible. |
| Riesgo operativo | 10 % | 3 | 4 | 2 | C tiene más puntos de falla. |
| Consistencia de inventario y pedidos | 15 % | 4 | 5 | 2 | En A y B una transacción evita la sobreventa; en C hay que coordinar servicios. |
| Escalabilidad ante picos | 10 % | 2 | 3 | 5 | C escala por módulo; B replica todo el sistema. |
| Testabilidad | 10 % | 2 | 5 | 3 | En B la lógica se prueba con puertos falsos, sin infraestructura. |
| Alineación con negocio y equipo | 5 % | 3 | 5 | 2 | B refleja los dominios del negocio y encaja con un equipo pequeño. |
| **Puntaje ponderado** | **100 %** | **3,50** | **4,10** | **2,70** | |

Cálculo de B: 4×0,15 + 4×0,10 + 3×0,10 + 4×0,15 + 4×0,10 + 5×0,15 + 3×0,10 + 5×0,10 + 5×0,05 = **4,10**.

## Análisis de sensibilidad
Una decisión que cambia con solo mover los pesos no es sólida, así que probé dos escenarios:

- **Crecimiento alto:** escalabilidad 25 %, evolución 20 %, consistencia 15 %, complejidad 10 %, riesgo 10 %, costo 5 %, rapidez 5 %, testabilidad 5 %, alineación 5 %.
- **Presupuesto austero:** complejidad 20 %, costo 20 %, rapidez 20 %, riesgo 10 %, consistencia 10 %, evolución 5 %, escalabilidad 5 %, testabilidad 5 %, alineación 5 %.

| Opción | Base | Crecimiento alto | Presupuesto austero |
|---|---|---|---|
| A: Capas | 3,50 | 3,05 | **4,15** |
| B: Monolito modular | **4,10** | **3,95** | 3,95 |
| C: Microservicios | 2,70 | 3,30 | 2,15 |

B gana en los dos primeros escenarios y queda muy cerca de A en el tercero. Si lo único que importara fuera el costo y la rapidez, ganaría A, pero eso ignoraría la evolución, la testabilidad y la consistencia, que son justo los riesgos del caso (R1, R2 y R6). Microservicios mejora cuando se premia el crecimiento, pero nunca alcanza a B.

## Respuesta de cada opción a los escenarios clave
| Escenario | A: Capas | B: Monolito modular | C: Microservicios |
|---|---|---|---|
| QS-02 Pico de 600 pedidos/hora | Cumple con más recursos. | Cumple replicando el sistema; la cifra es moderada. | Cumple y escala por módulo. |
| QS-03 Sin sobreventa | Cumple. | Cumple con una transacción simple. | Difícil: exige coordinar servicios. |
| QS-08 Falla de un externo | Riesgo de que se propague. | Cumple con adaptadores aislados. | Cumple. |
| QS-11 Trazabilidad | Cumple. | Cumple. | Más difícil: hay que reunir eventos de varios servicios. |
| QS-12 Cambio de regla de asignación | Toca varias capas. | Cumple si se respetan los límites. | Cumple. |
| QS-15 Pruebas sin infraestructura | No cumple: la lógica depende de la base de datos. | Cumple con puertos falsos. | Cumple dentro de cada servicio. |

## Conclusión
La opción **B** equilibra mejor los requisitos que más pesan (consistencia, trazabilidad, evolución y testabilidad) con las restricciones reales: equipo pequeño, presupuesto limitado y volumen moderado. La opción C resuelve la escalabilidad, pero hoy no hay evidencia de que la necesitemos. La opción A es la más rápida, pero deja sin resolver el crecimiento desordenado. La decisión formal está en el [ADR-001](06-adr-001-monolito-modular.md).
