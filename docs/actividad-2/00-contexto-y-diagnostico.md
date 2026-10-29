# 00. Contexto y diagnóstico del caso

Este documento resume el problema, los actores, el alcance y los riesgos del caso. Es el punto de partida de la Actividad 2: los requisitos y la decisión estructural salen de aquí.

> Proyecto académico y ficticio, inspirado en la comunidad de Roblox. No está afiliado a Roblox Corporation.

## El caso
**PixelPack** es una empresa de retail digital (ficticia) que vende productos físicos temáticos de Roblox: peluches, figuras de colección, ropa, accesorios gamer y tarjetas de regalo físicas. Su promesa diferencial es la **entrega el mismo día, en 3 horas o menos, dentro de su zona de cobertura**, saliendo desde varias **micro-bodegas** repartidas por la ciudad y con repartidores independientes (freelance).

**PixelRoute** es la plataforma de gestión logística de última milla que PixelPack necesita para cumplir esa promesa.

## Problema
PixelPack recibe pedidos desde varios canales (tienda web, app móvil, servidor de Discord, redes sociales y marketplaces), tiene el inventario repartido en 6 micro-bodegas y coordina entregas con repartidores independientes. No cuenta con una vista centralizada del estado real de cada pedido, ni sabe en qué micro-bodega conviene preparar cada uno ni qué repartidor está disponible. Esto genera duplicación de información, entregas que se pasan de las 3 horas prometidas, errores operativos y poca trazabilidad. Los clientes (muchos de ellos menores que compran con sus familias) no saben cuándo llegará su pedido.

## Actores
| Actor | Tipo | Rol |
|---|---|---|
| Cliente (jugador o familia) | Usuario | Compra, rastrea su pedido y reporta problemas |
| Operador de tienda | Usuario | Registra y valida pedidos, prepara despachos |
| Administrador de almacén (micro-bodega) | Usuario | Controla el inventario y la salida de pedidos de su bodega |
| Repartidor independiente | Usuario | Acepta pedidos, entrega y reporta el estado |
| Soporte al cliente | Usuario | Atiende reclamos y gestiona incidencias |
| Canales de venta | Externo | Envían los pedidos |
| Sistema de pagos | Externo | Confirma pagos y reembolsos |
| Servicio de geolocalización | Externo | Direcciones, rutas y ubicación |
| Sistema de notificaciones | Externo | Envía avisos al cliente |
| ERP / CRM interno | Externo | Datos maestros de clientes, productos y facturación |

## Alcance
**Dentro:** registro de pedidos de todos los canales, validación y reserva de inventario por micro-bodega, elección de la bodega de despacho, asignación de repartidores, seguimiento de la entrega, gestión de incidencias y notificaciones al cliente.

**Fuera (por ahora):** elección de tecnologías, optimización avanzada de rutas con algoritmos, procesamiento interno de pagos, reemplazo del ERP/CRM y la entrega de productos digitales de Roblox (Robux, ítems virtuales). Roblox es el tema del negocio, no una integración técnica.

## Riesgos del diagnóstico
| ID | Riesgo |
|---|---|
| R1 | Duplicación de pedidos (el mismo pedido entra por dos canales). |
| R2 | Inconsistencias de inventario entre micro-bodegas, sobre todo en ediciones limitadas. |
| R3 | Entregas fallidas o fuera de la promesa de 3 horas. |
| R4 | Pérdida de trazabilidad. |
| R5 | Mala coordinación entre micro-bodegas, flota de repartidores y soporte. |
| R6 | Crecimiento sin control: picos en lanzamientos y temporadas altas. |
| R7 | Seguridad y manejo de datos personales, incluidos los de menores. |
| R8 | Dependencia de sistemas externos (pagos, geolocalización, notificaciones, ERP/CRM). |
| R9 | Fraude y suplantación, incluidos repartidores independientes no confiables. |
| R10 | Disponibilidad variable de los repartidores independientes. |

## Decisión inicial heredada del diagnóstico
Una plataforma **centralizada** que registre pedidos, valide inventario, elija la micro-bodega, asigne repartidores, rastree entregas y gestione incidencias desde un único sistema. La Actividad 2 define **cómo se estructura** esa plataforma.
