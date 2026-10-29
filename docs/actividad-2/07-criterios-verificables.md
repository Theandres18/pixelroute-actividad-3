# 07. Criterios verificables

Sirven para revisar, con evidencia y no con opiniones, si la decisión del [ADR-001](06-adr-001-monolito-modular.md) responde a las necesidades del sistema.

## Criterios de la estructura
| ID | Criterio | Cómo se verifica | Umbral | Relacionado con |
|---|---|---|---|---|
| CV-01 | Los módulos no dependen de detalles internos de otros. | Prueba de arquitectura automática sobre las dependencias entre módulos. | **0 violaciones** de las reglas de acoplamiento. | Doc. 04, RNF-09 |
| CV-02 | No hay dependencias circulares entre módulos. | Análisis de dependencias del repositorio. | **0 ciclos**. | Doc. 04 |
| CV-03 | Cada módulo es dueño de sus datos. | Revisión de consultas y del esquema de datos. | **0 consultas** que lean tablas de otro módulo. | RNF-01 |
| CV-04 | La lógica de negocio depende de puertos, no de infraestructura. | Revisar que los casos de uso no importen clases de base de datos, mapas ni mensajería. | **0 referencias** a infraestructura concreta en los casos de uso. | RNF-10, QS-15 |
| CV-05 | Los adaptadores no contienen reglas de negocio. | Revisión de código de cada adaptador. | **0 reglas de negocio** en adaptadores. | RNF-05, RNF-11 |

## Criterios de calidad (escenarios)
| ID | Criterio | Cómo se verifica | Umbral | Escenario |
|---|---|---|---|---|
| CV-06 | Consulta de estado rápida. | Prueba de carga con 300 consultas simultáneas. | p95 **menor a 2 s**. | QS-01 |
| CV-07 | Soporta el pico de un lanzamiento. | Prueba de carga a 600 pedidos por hora (≈ 3.000 transacciones). | **0 pedidos perdidos**; registro con p95 **menor a 3 s**. | QS-02 |
| CV-08 | No hay sobreventa. | Prueba de concurrencia: 100 solicitudes simultáneas por 1 unidad. | **1 confirmado**, 99 rechazados, 0 sobreventas. | QS-03 |
| CV-09 | Se detectan duplicados. | Conjunto de pruebas con pedidos duplicados por varios canales. | **95 % o más** detectados; **0 despachados**. | QS-04 |
| CV-10 | La asignación es rápida. | Simulación de 100 pedidos con repartidores disponibles. | Primera oferta en **menos de 60 s** en el 95 % de los casos. | QS-05 |
| CV-11 | Se alerta a tiempo un pedido atrasado. | Simular pedidos sin recoger durante 90 minutos. | Alerta en **menos de 1 min**, en el **100 %** de los casos. | QS-06 |
| CV-12 | Resiste la caída de un externo. | Simulacro: apagar el adaptador de geolocalización 30 minutos. | **0 pedidos bloqueados**; recuperación automática. | QS-08 |
| CV-13 | Control de acceso por rol. | Pruebas por rol (cliente, repartidor, soporte, almacén, operador). | **0 accesos no autorizados**; **100 %** de intentos registrados. | QS-09 |
| CV-14 | Autorización de acudientes. | Pruebas con pedidos de menores sin autorización. | **100 %** bloqueados hasta registrar la autorización. | QS-10 |
| CV-15 | Trazabilidad completa. | Revisar el historial de pedidos de prueba de punta a punta. | **100 %** de cambios con quién, cuándo y desde dónde; consulta en **menos de 3 s**. | QS-11 |
| CV-16 | Cambiar la regla de asignación afecta un solo módulo. | Cambiar la regla y revisar el diff. | **1 módulo** modificado, **1 día o menos**. | QS-12 |
| CV-17 | Agregar un canal no toca el núcleo. | Agregar un canal de prueba y revisar el diff. | **0 módulos del núcleo** modificados. | QS-13 |
| CV-18 | Trabajo sin conexión del repartidor. | Simular entregas sin red y reconectar. | **0 entregas perdidas**; sincronización en **menos de 1 min**. | QS-14 |
| CV-19 | Pruebas de casos de uso sin infraestructura. | Ejecutar la suite de pruebas del caso de uso "Crear pedido" con puertos falsos. | Corre en **menos de 10 s** sin servicios externos. | QS-15 |

## Criterios de la decisión (revisión periódica)
| ID | Criterio | Cómo se verifica | Umbral | Cuándo |
|---|---|---|---|---|
| CV-20 | La decisión sigue siendo adecuada. | Revisar los 5 criterios de reevaluación del ADR-001. | Ninguno se cumple de forma sostenida. | Cada trimestre y después de cada lanzamiento grande |
| CV-21 | Los supuestos de volumen siguen vigentes. | Comparar el volumen real con S1 y S2. | Dentro de **±50 %** de lo supuesto. | Cada trimestre |

## Trazabilidad: de los riesgos a la verificación
| Riesgo | Requisitos | Escenarios | Criterios |
|---|---|---|---|
| R1 Duplicación de pedidos | RF-01, RF-02, RNF-01 | QS-04 | CV-09 |
| R2 Inconsistencias de inventario | RF-05, RF-06, RF-07, RNF-01 | QS-03 | CV-08, CV-03 |
| R3 Entregas fuera de las 3 horas | RF-09 a RF-13, RF-17, RNF-02 | QS-05, QS-06, QS-07 | CV-10, CV-11 |
| R4 Pérdida de trazabilidad | RF-03, RF-12, RNF-08 | QS-11 | CV-15 |
| R5 Mala coordinación entre áreas | RF-03, RF-15, RF-16 | QS-11 | CV-15 |
| R6 Crecimiento sin control | RF-08, RNF-03, RNF-09, RNF-10 | QS-02, QS-12, QS-15 | CV-07, CV-16, CV-19, CV-20 |
| R7 Seguridad y datos de menores | RF-21, RF-22, RNF-06, RNF-07 | QS-09, QS-10 | CV-13, CV-14 |
| R8 Dependencia de externos | RF-20, RNF-05, RNF-11 | QS-08, QS-13 | CV-12, CV-17, CV-05 |
| R9 Fraude y repartidores no confiables | RF-12, RF-14, RF-19, RNF-06 | QS-09 | CV-13 |
| R10 Disponibilidad variable de repartidores | RF-10, RF-11, RF-14, RF-17 | QS-05, QS-14 | CV-10, CV-18 |

## Checklist: ¿la decisión responde a las necesidades?
- [ ] Los requisitos *Must* están cubiertos por algún módulo del monolito.
- [ ] Los escenarios QS-01 a QS-15 tienen un criterio que los comprueba.
- [ ] Los 10 riesgos del diagnóstico aparecen en la tabla de trazabilidad.
- [ ] Los límites entre módulos se comprueban de forma automática (CV-01 a CV-05).
- [ ] Están definidos los criterios para reevaluar la decisión (CV-20 y CV-21).
