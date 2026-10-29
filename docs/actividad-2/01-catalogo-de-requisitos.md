# 01. Catálogo de requisitos

Cada requisito sale del [diagnóstico](00-contexto-y-diagnostico.md) y está pensado para atacar uno o varios de los riesgos R1 a R10.

**Convenciones:** `RF` = requisito funcional, `RNF` = requisito no funcional (atributo de calidad), `RES` = restricción, `S` = supuesto de trabajo.

## Supuestos de trabajo (a confirmar con el negocio)
No tenemos datos reales de PixelPack, así que para poder escribir escenarios con números concretos asumí lo siguiente:

| ID | Supuesto |
|---|---|
| S1 | En operación normal, PixelPack recibe hasta **150 pedidos por hora** en horas pico. |
| S2 | En un lanzamiento o temporada alta se llega hasta **600 pedidos por hora** (4 veces lo normal). Cada pedido genera unas 5 transacciones (registro, reserva, asignación, cambios de estado, notificación), es decir, cerca de **3.000 transacciones por hora**. |
| S3 | La operación va de **8:00 a 22:00**. |
| S4 | Hay **4 canales de venta**, **6 micro-bodegas** y unos **40 repartidores independientes** activos. |
| S5 | La promesa al cliente es entregar en **3 horas o menos** dentro de la zona de cobertura. |
| S6 | El equipo de desarrollo es **pequeño** (3 a 5 personas) y no tiene un equipo dedicado de infraestructura. |
| S7 | Los repartidores usan **celular** y a veces pierden conexión. |

## Requisitos funcionales

### Pedidos
| ID | Requisito | Actor | Riesgo |
|---|---|---|---|
| RF-01 | Registrar pedidos de cualquier canal en un único registro. | Cliente, operador | R1, R4 |
| RF-02 | Detectar posibles pedidos duplicados antes de confirmarlos. | Sistema | R1 |
| RF-03 | Consultar el estado actual y el historial completo de un pedido. | Cliente, soporte, operador | R4, R5 |
| RF-04 | Cancelar o modificar un pedido mientras no haya empezado su preparación. | Cliente, operador | R3 |

### Inventario
| ID | Requisito | Actor | Riesgo |
|---|---|---|---|
| RF-05 | Validar la disponibilidad del producto en las micro-bodegas cercanas al cliente. | Sistema | R2 |
| RF-06 | Reservar el stock en la micro-bodega elegida al confirmar el pedido. | Sistema | R2 |
| RF-07 | Registrar ingresos, salidas, traslados entre micro-bodegas y devoluciones. | Administrador de almacén | R2 |
| RF-08 | Generar alertas de bajo stock y sugerir reposición. | Administrador de almacén | R2, R6 |

### Despacho y flota
| ID | Requisito | Actor | Riesgo |
|---|---|---|---|
| RF-09 | Elegir la micro-bodega de despacho según cercanía al cliente y stock disponible. | Sistema | R3 |
| RF-10 | Asignar automáticamente el pedido a un repartidor disponible cercano. | Sistema | R3, R10 |
| RF-11 | Que el repartidor acepte o rechace una oferta de pedido y consulte su ruta. | Repartidor | R3, R10 |
| RF-12 | Que el repartidor actualice el estado de la entrega con evidencia (foto o código de confirmación). | Repartidor | R4, R9 |
| RF-13 | Que el cliente rastree su pedido en un mapa con la hora estimada de llegada. | Cliente | R3, R4 |
| RF-14 | Gestionar la disponibilidad y el perfil de los repartidores independientes. | Administrador | R9, R10 |

### Incidencias y soporte
| ID | Requisito | Actor | Riesgo |
|---|---|---|---|
| RF-15 | Registrar incidencias (retraso, entrega fallida, producto equivocado, reclamo). | Repartidor, cliente, soporte | R3, R5 |
| RF-16 | Que soporte consulte la trazabilidad completa y gestione las incidencias hasta cerrarlas. | Soporte | R4, R5 |
| RF-17 | Reasignar o reprogramar una entrega fallida o sin repartidor. | Soporte, sistema | R3, R10 |

### Transversales e integración
| ID | Requisito | Actor | Riesgo |
|---|---|---|---|
| RF-18 | Notificar al cliente cada cambio importante de estado. | Sistema | R3 |
| RF-19 | Integrarse con el sistema de pagos para confirmar pagos y gestionar reembolsos. | Sistema | R1, R9 |
| RF-20 | Sincronizar clientes, productos e inventario maestro con el ERP/CRM. | Sistema | R2, R8 |
| RF-21 | Autenticar usuarios y controlar el acceso según su rol. | Todos | R7, R9 |
| RF-22 | Registrar la autorización del acudiente cuando el cliente es menor de edad. | Cliente, soporte | R7 |
| RF-23 | Mostrar reportes operativos (entregas dentro de las 3 horas, fallidas, incidencias abiertas). | Administrador | R6 |

## Requisitos no funcionales (atributos de calidad)
| ID | Atributo | Descripción | Riesgo |
|---|---|---|---|
| RNF-01 | Consistencia de inventario y pedidos | Nunca se vende más de lo que existe en una micro-bodega ni se procesa dos veces el mismo pedido. | R1, R2 |
| RNF-02 | Rendimiento | Las consultas responden rápido y los pedidos se asignan a un repartidor en poco tiempo. | R3 |
| RNF-03 | Escalabilidad ante picos | El sistema aguanta lanzamientos y temporadas altas sin degradarse. | R6 |
| RNF-04 | Disponibilidad | El sistema está operativo durante el horario de entregas. | R3 |
| RNF-05 | Resiliencia ante fallas externas | Si falla pagos, geolocalización, notificaciones o el ERP, la operación central sigue funcionando. | R8 |
| RNF-06 | Seguridad y control de acceso | Cada rol ve y hace solo lo que le corresponde (por ejemplo, el repartidor ve la dirección completa solo al aceptar). | R7, R9 |
| RNF-07 | Privacidad y datos de menores | Los datos personales y de ubicación están protegidos, con autorización de acudientes para menores. | R7 |
| RNF-08 | Trazabilidad y auditabilidad | Cada cambio de estado deja registro de quién, cuándo y desde dónde. | R4, R5 |
| RNF-09 | Mantenibilidad y evolución | Cambiar una regla de negocio o agregar un canal no obliga a reescribir el sistema. | R6 |
| RNF-10 | Testabilidad | La lógica de negocio se puede probar sin base de datos ni servicios externos. | R6, R8 |
| RNF-11 | Interoperabilidad | La integración con sistemas externos se hace con contratos claros y reemplazables. | R8 |
| RNF-12 | Usabilidad móvil y baja conectividad | El repartidor puede trabajar con conexión intermitente. | R3, R10 |

## Restricciones
| ID | Restricción |
|---|---|
| RES-01 | Se integra con el ERP/CRM existente; no se reemplaza. |
| RES-02 | Pagos, geolocalización y notificaciones se consumen como servicios externos. |
| RES-03 | Se debe cumplir la protección de datos personales (Ley 1581 de 2012 en Colombia), con atención especial a menores. |
| RES-04 | Equipo pequeño y presupuesto limitado (S6): la solución no puede exigir una operación compleja de infraestructura. |
| RES-05 | Los repartidores son independientes, no empleados: la plataforma no puede asumir que siempre estarán disponibles. |
| RES-06 | Esta actividad define la estructura, no las tecnologías. Las herramientas se eligen en las siguientes unidades. |
