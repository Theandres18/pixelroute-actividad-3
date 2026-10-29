# 02. Matriz de priorización

## Cómo prioricé
- **Prioridad (MoSCoW):** *Must* si sin el requisito no se resuelve el problema o hay un riesgo legal; *Should* si aporta mucho valor pero el sistema podría arrancar sin él; *Could* si es deseable y se puede dejar para después.
- **Impacto en negocio (A/M/B):** cuánto afecta ventas, experiencia del cliente o la promesa de 3 horas.
- **Impacto en arquitectura (A/M/B):** cuánto condiciona la estructura. Si cambiarlo después es caro, el impacto es alto.
- **Complejidad (A/M/B):** esfuerzo y dificultad estimados.

A = Alto, M = Medio, B = Bajo.

## Requisitos funcionales
| ID | Requisito (resumen) | Prioridad | Negocio | Arquitectura | Complejidad | Justificación |
|---|---|---|---|---|---|---|
| RF-01 | Registro único de pedidos | Must | A | A | M | Es la base de la vista centralizada. |
| RF-02 | Detección de duplicados | Must | A | M | M | Ataca R1 directamente. |
| RF-03 | Estado e historial del pedido | Must | A | M | B | Es el dolor principal del cliente y de soporte. |
| RF-04 | Cancelar o modificar pedido | Should | M | B | M | Útil, pero no bloquea el arranque. |
| RF-05 | Validar disponibilidad por micro-bodega | Must | A | A | M | Evita vender producto que no está cerca. |
| RF-06 | Reservar stock | Must | A | A | A | Requiere control de concurrencia; es crítico en ediciones limitadas. |
| RF-07 | Movimientos de inventario | Must | A | M | M | Sin esto el inventario no refleja la realidad. |
| RF-08 | Alertas de bajo stock | Should | M | B | M | Mejora la reposición, pero se puede hacer manual al inicio. |
| RF-09 | Elegir micro-bodega de despacho | Must | A | M | A | De esto depende cumplir las 3 horas. |
| RF-10 | Asignar repartidor automáticamente | Must | A | A | A | Es el corazón de la última milla con flota independiente. |
| RF-11 | Aceptar o rechazar oferta y ver ruta | Must | A | M | M | Sin esto el repartidor no puede trabajar. |
| RF-12 | Estado de entrega con evidencia | Must | A | M | M | Da trazabilidad y reduce fraude. |
| RF-13 | Rastreo en mapa para el cliente | Must | A | M | M | Resuelve que el cliente no sabe cuándo llega su pedido. |
| RF-14 | Gestión de repartidores | Should | M | M | M | Se puede arrancar con una lista manual. |
| RF-15 | Registro de incidencias | Must | A | M | M | Sin esto los problemas no se gestionan. |
| RF-16 | Gestión de incidencias por soporte | Must | A | M | M | Reduce la mala coordinación entre áreas. |
| RF-17 | Reasignar o reprogramar | Should | M | B | M | Importante, pero soporte puede hacerlo manual al inicio. |
| RF-18 | Notificaciones de estado | Must | A | M | M | Reduce consultas a soporte. |
| RF-19 | Integración con pagos | Must | A | M | M | Sin pago confirmado no hay pedido válido. |
| RF-20 | Sincronización con ERP/CRM | Must | A | A | A | Es una restricción y afecta el modelo de datos. |
| RF-21 | Autenticación y roles | Must | A | A | M | Condiciona todo el acceso; cambiarlo tarde es caro. |
| RF-22 | Autorización de acudientes | Must | A | M | M | Es un requisito legal por los menores. |
| RF-23 | Reportes operativos | Could | B | B | B | Útil, pero se puede construir después. |

**Resumen:** 18 *Must*, 4 *Should* y 1 *Could*.

## Requisitos no funcionales
| ID | Atributo | Prioridad | Negocio | Arquitectura | Complejidad | Justificación |
|---|---|---|---|---|---|---|
| RNF-01 | Consistencia de inventario y pedidos | Must | A | A | A | Si falla, se vende producto que no existe. Condiciona cómo se guardan los datos. |
| RNF-02 | Rendimiento | Must | A | M | M | Afecta la experiencia y el cumplimiento de las 3 horas. |
| RNF-03 | Escalabilidad ante picos | Must | A | A | A | Los lanzamientos concentran las ventas y el riesgo. |
| RNF-04 | Disponibilidad | Must | A | M | M | Una caída en horario de entregas paraliza la operación. |
| RNF-05 | Resiliencia ante fallas externas | Must | A | A | M | Hay cuatro sistemas externos; alguno va a fallar. |
| RNF-06 | Seguridad y control de acceso | Must | A | A | M | Hay datos personales, varios roles y repartidores externos. |
| RNF-07 | Privacidad y datos de menores | Must | A | A | M | Requisito legal y de confianza. |
| RNF-08 | Trazabilidad y auditabilidad | Must | A | A | M | Es el objetivo central del proyecto. |
| RNF-09 | Mantenibilidad y evolución | Must | A | A | M | El negocio va a crecer y cambiar sus reglas. |
| RNF-10 | Testabilidad | Must | M | A | M | Es lo que permite cambiar reglas sin miedo; condiciona cómo se separa la lógica de la infraestructura. |
| RNF-11 | Interoperabilidad | Should | M | A | M | Se cubre en buena parte con RNF-05 y RNF-09. |
| RNF-12 | Usabilidad móvil y baja conectividad | Should | M | M | M | Afecta más a la interfaz que a la estructura. |

## Requisitos arquitectónicamente significativos
No todos los requisitos pesan igual en la estructura. Los que más la condicionan, con impacto alto en negocio y en arquitectura, son:

| Orden | Requisito | Por qué condiciona la estructura |
|---|---|---|
| 1 | RNF-01 Consistencia de inventario y pedidos | Pedidos e inventario deben actualizarse de forma coordinada y segura. |
| 2 | RNF-03 Escalabilidad ante picos | Define si hace falta escalar partes por separado. |
| 3 | RF-10 Asignación automática de repartidores | Es el proceso más sensible al tiempo y a los cambios de reglas. |
| 4 | RNF-08 Trazabilidad | Exige un historial único y confiable de eventos. |
| 5 | RNF-06 y RNF-07 Seguridad y privacidad | Exigen un punto claro de control de acceso y manejo de datos personales. |
| 6 | RNF-09 y RNF-10 Evolución y testabilidad | Exigen límites claros y lógica de negocio aislada de la infraestructura. |
| 7 | RNF-05 Resiliencia ante fallas externas | Exige aislar las integraciones del núcleo. |

Estos siete puntos son los criterios que más pesaron en la comparación de alternativas.
