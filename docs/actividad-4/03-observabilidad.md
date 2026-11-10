# 03. Observabilidad

El objetivo es poder **diagnosticar el flujo sin exponer datos sensibles**. Todas las señales llevan identificadores técnicos y resultados; ninguna lleva direcciones, claves ni contenido de mensajes.

## Señales definidas

### Registros (logs) estructurados
Cada registro de una solicitud incluye el `CorrelationId` (la consola lo muestra porque `IncludeScopes` está activo).

| Evento | Nivel | Campos | Dónde |
|---|---|---|---|
| Entrega creada | Information | `DeliveryId` | API |
| Repartidor asignado | Information | `CourierId`, `DeliveryId` | API |
| Asignación rechazada | Warning | `DeliveryId`, `ErrorCode` | API |
| Solicitud rechazada por una regla o validación | Information | `ErrorCode`, `Path` | Middleware de errores |
| Error inesperado | Error | excepción (solo en el servidor), `Method`, `Path` | Middleware de errores |
| Falló un envío de aviso | Warning | `DeliveryId`, `Attempt`, `MaxAttempts`, `ErrorType` | Publicador |
| Aviso enviado | Information | `DeliveryId`, `Attempt` | Publicador |
| Aviso en la cola de fallidos | Error | `DeliveryId`, `Attempts` | Publicador |
| Migración aplicada o falló | Information o Error | `Version` | Ejecutor de migraciones |

### Métricas (`Meter` "PixelRoute")
| Métrica | Tipo | Etiquetas | Para qué sirve |
|---|---|---|---|
| `pixelroute.assignments.total` | Contador | `result` (`ok`, `courier_route_saturated`, `delivery_already_assigned`...) | Ver cuántas asignaciones salen bien y por qué se rechazan |
| `pixelroute.assignments.duration` | Histograma (ms) | n/a | Detectar lentitud (QS-01 y QS-02) |
| `pixelroute.notifications.sent` | Contador | n/a | Avisos entregados |
| `pixelroute.notifications.retries` | Contador | n/a | Señal temprana de que el servicio de avisos está inestable |
| `pixelroute.notifications.dead_lettered` | Contador | n/a | Avisos que requieren atención de operaciones |

### Trazas (`ActivitySource` "PixelRoute")
Un segmento `AssignCourier` por solicitud, con el identificador de la entrega como etiqueta. Quedan listas para exportarse con OpenTelemetry sin cambiar el código del negocio.

### Salud
`GET /health` devuelve 200 si la aplicación está viva. No requiere clave.

### Correlación
`X-Correlation-Id`: si el cliente lo envía y es seguro (letras, números, `-`, `_`, máximo 64 caracteres), se conserva; si no, se genera uno. Siempre se devuelve en la respuesta, también en los errores.

## Cómo ver las señales

**Registros:** aparecen en la terminal donde corre la API.

**Métricas (opcional):** con la API corriendo, en otra terminal:
```powershell
dotnet tool install --global dotnet-counters
dotnet-counters monitor --name PixelRoute.Api --counters PixelRoute
```
(si no reconoce el nombre del proceso, usa `dotnet-counters ps` para ver el correcto).

## Cómo diagnosticar un fallo: ejemplo
Un cliente reclama que no le llegó el aviso del repartidor.

1. Pedir el `X-Correlation-Id` de la solicitud (o buscar la entrega por su identificador).
2. Buscar en los registros ese identificador: se ven los registros de esa solicitud en orden.
3. Si hay `Falló el envío del aviso ... (intento 1 de 3)`, el servicio de avisos estaba caído.
4. Si hay `pasó a la cola de mensajes fallidos`, consultar `GET /api/v1/admin/dead-letters` para ver el mensaje y el tipo de error.
5. Comprobar `pixelroute.notifications.retries` y `dead_lettered` para saber si el problema es aislado o general.
6. Una vez recuperado el servicio, reenviar el aviso manualmente (el reenvío automático es un próximo paso).

## Experimento de caos controlado
**Hipótesis:** si el servicio de notificaciones deja de responder, las asignaciones siguen funcionando y los avisos quedan registrados para atenderlos después.

**Cómo ejecutarlo:**
1. Arrancar la API simulando la falla:
```powershell
$env:Notifications__Mode = "AlwaysFail"
dotnet run --project src/Api
```
2. En otra terminal, ejecutar `./scripts/demo.ps1`.
3. Observar en la terminal de la API: tres advertencias por cada asignación (intentos 1, 2 y 3) y un error final por la cola de fallidos.
4. En el paso 9 de la demostración, ver los mensajes en la cola de fallidos.

**Resultado esperado:** todas las asignaciones responden 200; la cola de mensajes fallidos crece en un mensaje por asignación; ninguna respuesta pierde datos. Esto está automatizado en la prueba `Si_el_servicio_de_avisos_cae_la_asignacion_sigue_y_el_aviso_queda_en_la_cola_de_fallidos`.

**Variante:** con `Notifications__Mode = "FailTwiceThenOk"` el aviso se envía en el tercer intento y la cola queda vacía.

**Para volver a la normalidad:** cerrar la terminal, o `Remove-Item Env:Notifications__Mode`.
