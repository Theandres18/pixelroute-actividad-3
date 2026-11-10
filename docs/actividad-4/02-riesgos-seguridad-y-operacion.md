# 02. Riesgos operativos y de seguridad

## Cómo los encontré: pre-mortem y cinco porqués
Apliqué la técnica del pre-mortem (videos 11 y 12): imaginé que dentro de unos meses el flujo de asignación **falló en producción** y me pregunté por qué pudo pasar. De ahí salió la lista de abajo, y cada riesgo importante terminó convertido en una prueba verificable.

**Ejemplo de cinco porqués (aviso al cliente que nunca llegó):**
1. ¿Por qué el cliente no recibió el aviso? Porque el servicio de notificaciones no respondió.
2. ¿Por qué no se reenvió? Porque el sistema no reintentaba.
3. ¿Por qué no se supo que falló? Porque el error no se registraba ni se guardaba.
4. ¿Por qué? Porque se asumió que el servicio externo siempre está disponible.
5. **Causa raíz:** no se diseñó la integración externa pensando en que va a fallar. → Reintentos, cola de mensajes fallidos y una prueba que apaga el servicio (RO-01).

**Ejemplo de cinco porqués (repartidor con demasiadas entregas):**
1. ¿Por qué un repartidor tiene más entregas que su máximo? Porque dos asignaciones se procesaron a la vez.
2. ¿Por qué ambas pasaron? Porque las dos leyeron el mismo conteo de entregas activas.
3. ¿Por qué no se detectó? Porque la verificación y el guardado no son una sola operación atómica.
4. ¿Por qué? Porque el control de versión protege cada entrega, no el cupo del repartidor.
5. **Causa raíz:** la regla de cupo involucra a dos agregados (entrega y repartidor). → Riesgo residual RO-12, con mitigación propuesta.

## Registro de riesgos

Escala: probabilidad (P) e impacto (I) en Alta (A), Media (M) o Baja (B).

| ID | Riesgo | Tipo | P | I | Mitigación implementada | Evidencia de verificación | Riesgo residual |
|---|---|---|---|---|---|---|---|
| RO-01 | El servicio de notificaciones cae y el cliente no recibe el aviso | Operación | A | M | Reintentos con espera exponencial y cola de mensajes fallidos; el fallo no deshace la asignación | `Reintenta_y_tiene_exito_en_el_tercer_intento`, `Si_se_agotan_los_intentos...`, `Si_el_servicio_de_avisos_cae_la_asignacion_sigue...` | Los mensajes fallidos se reenvían a mano |
| RO-02 | El proceso se cae entre guardar la entrega y publicar el evento (doble escritura) | Operación | B | M | El evento se publica después de guardar; el fallo del servicio externo no pierde el mensaje (RO-01) | Revisión de `AssignCourierUseCase` | **Sí**: un corte exacto entre ambos pasos pierde el aviso. Próximo paso: outbox transaccional |
| RO-03 | Dos asignaciones simultáneas de la misma entrega | Integridad | M | A | Control de concurrencia optimista (`version`); la API responde 409 | `Dos_asignaciones_concurrentes_de_la_misma_entrega...`, `Asignar_una_entrega_que_ya_tiene_repartidor_devuelve_409` | Resuelto para la misma entrega |
| RO-04 | Una migración falla o deja el esquema a medias | Operación | M | A | Cada script corre en una transacción; si falla, se deshace y la aplicación no arranca; migraciones registradas y sin repetirse | `Aplica_el_esquema_y_ejecutarlo_otra_vez_no_repite_nada` | No hay migraciones de reversa |
| RO-05 | Alguien sin permiso asigna repartidores | Seguridad | M | A | Clave de despachador obligatoria, comparada en tiempo constante; si no está configurada, todo se rechaza | `Sin_la_clave_de_despachador_se_rechaza_con_401...` | Una sola clave compartida; no hay roles |
| RO-06 | Datos personales (la dirección) o secretos aparecen en los registros | Seguridad y privacidad | M | A | Los registros solo llevan identificadores, resultados y tipos de error; la consulta de entregas no devuelve la dirección | `Los_registros_no_contienen_la_direccion_ni_la_clave`, `Consultar_devuelve_la_vista_sin_la_direccion` | Hay que mantener la disciplina en cada registro nuevo |
| RO-07 | Un error inesperado revela detalles internos al cliente | Seguridad | M | M | Respuesta genérica con identificador de correlación; el detalle queda solo en el servidor | `Un_error_inesperado_devuelve_500_generico_sin_detalles_internos` | Ninguno relevante |
| RO-08 | Inyección de SQL a través de un dato | Seguridad | B | A | Solo consultas parametrizadas; el esquema rechaza datos inválidos | `Un_texto_malicioso_se_guarda_como_dato_y_no_se_ejecuta` | Ninguno relevante |
| RO-09 | Los datos de demostración llegan a producción | Operación | B | M | `Database:SeedDemoData` es `false` por defecto y los datos van en una carpeta aparte | `Los_datos_de_demostracion_solo_se_cargan_si_se_pide` | Depende de la configuración del entorno |
| RO-10 | Secretos en el repositorio | Seguridad | M | A | La clave de `appsettings.json` está vacía; la de desarrollo solo existe en el archivo de desarrollo; los archivos `.db` están en `.gitignore` | Revisión del repositorio | La clave de desarrollo es pública: no sirve más que para desarrollo |
| RO-11 | Un pico de pedidos supera lo que aguanta SQLite | Operación | M | A | Los puertos permiten cambiar la base de datos sin tocar el dominio | Ninguna: **no hay prueba de carga** | **Sí**: QS-02 (600 pedidos por hora) no está validado. Próximo paso: PostgreSQL y prueba de carga |
| RO-12 | Dos asignaciones de entregas **distintas** al mismo repartidor lo dejan por encima de su cupo | Integridad | M | M | Ninguna todavía; la verificación del cupo se hace con una lectura previa | Ninguna: es un riesgo conocido | **Sí**: falta una operación atómica. Próximo paso: actualización condicional o bloqueo por repartidor |
| RO-13 | Paquetes con vulnerabilidades conocidas | Seguridad | M | M | Revisión con `dotnet list package --vulnerable --include-transitive` | Resultado anotado en el [documento 04](04-pruebas-y-resultados.md) | Revisión manual, no automática |
| RO-14 | Abuso de la API (sin límite de solicitudes) | Seguridad y operación | M | M | Ninguna todavía | Ninguna | **Sí**: próximo paso, limitar solicitudes por clave |

## Información sensible y qué se excluye
| Dato | Dónde existe | Tratamiento |
|---|---|---|
| Dirección de entrega | Base de datos (`deliveries.address`) | No se registra en logs, no se devuelve en la consulta de entregas y no viaja en los avisos |
| Clave de despachador | Configuración | No se registra; se compara en tiempo constante |
| Identificadores (entrega, pedido, repartidor) | Base de datos, logs, métricas | Se permiten: son identificadores técnicos, no datos personales |
| Contenido de excepciones | Servidor | En el registro del servidor, nunca en la respuesta HTTP |
| Datos del cliente (nombre, teléfono) | No se manejan en este flujo | El sistema de notificaciones resuelve el destinatario a partir del pedido |

## Revisión de seguridad (checklist breve)
- [x] Entradas validadas (formato en la API, reglas en el dominio).
- [x] Consultas SQL parametrizadas.
- [x] Autenticación en todos los endpoints de operación; falla cerrada.
- [x] Comparación de claves en tiempo constante.
- [x] Errores sin detalles internos.
- [x] Registros sin datos personales ni secretos.
- [x] Identificador de correlación validado (evita la inyección de texto en logs).
- [ ] Limitación de solicitudes.
- [ ] Roles y expiración de credenciales (JWT).
- [ ] Análisis dinámico o pentest.
