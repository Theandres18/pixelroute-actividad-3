# PixelRoute: plataforma de logística de última milla para PixelPack

![CI](https://github.com/Theandres18/pixelroute-actividad-3/actions/workflows/ci.yml/badge.svg)

> **Actividad 4: Construcción, validación y defensa final**
> Proyecto académico y ficticio, inspirado en la comunidad de Roblox. No está afiliado a Roblox Corporation.

PixelPack es una empresa de retail digital (ficticia) que vende productos físicos temáticos de Roblox y promete **entregar en 3 horas o menos** desde 6 micro-bodegas, con repartidores independientes. PixelRoute centraliza pedidos, inventario, despacho, seguimiento e incidencias. El contexto viene de la [Actividad 2](docs/actividad-2/00-contexto-y-diagnostico.md) y el diseño de la [Actividad 3](docs/actividad-3/00-flujo-de-negocio.md).

## Qué está implementado
El flujo **asignar un repartidor a una entrega**, como un vertical slice ejecutable: API REST, caso de uso, dominio con invariantes, persistencia en SQLite con migraciones, aviso al cliente con reintentos y cola de mensajes fallidos, seguridad mínima y observabilidad.

| Método y ruta | Qué hace |
|---|---|
| `POST /api/v1/deliveries` | Crea una entrega Pendiente |
| `GET /api/v1/deliveries/{id}` | Consulta el estado de una entrega |
| `POST /api/v1/deliveries/{id}/assign-courier` | **Asigna un repartidor** (200, 404, 409, 422) |
| `GET /api/v1/admin/dead-letters` | Lista los avisos que no se pudieron enviar |
| `GET /health` | Estado del servicio (público) |

Todos los endpoints de `/api/v1` requieren el encabezado `X-Api-Key`.

## Requisitos
- SDK de .NET 10 (`dotnet --version` debe mostrar `10.x`).
- Para la demostración: PowerShell. Opcional: la extensión REST Client de VS Code.

## Cómo ejecutar

### 1. Compilar y probar
```powershell
dotnet build
dotnet test
```
Deben pasar las **55 pruebas**.

### 2. Arrancar la API
```powershell
dotnet run --project src/Api
```
Escucha en `http://localhost:5080`. En el primer arranque crea `pixelroute.db`, aplica las migraciones y carga tres repartidores de demostración (solo en el entorno de desarrollo).

| Repartidor | Identificador | Condición |
|---|---|---|
| Demo 1 | `11111111-1111-1111-1111-111111111111` | En línea, máximo 3 entregas |
| Demo 2 | `22222222-2222-2222-2222-222222222222` | Fuera de línea |
| Demo 3 | `33333333-3333-3333-3333-333333333333` | En línea, máximo 1 entrega |

La clave de despachador de desarrollo es `dev-dispatcher-key`.

### 3. Ejecutar la demostración
En **otra** terminal, con la API corriendo:
```powershell
./scripts/demo.ps1
```
Muestra el camino correcto y los casos de error y de límite (409, 422, 404 y 401). También hay peticiones de ejemplo en `requests.http`.

### 4. Simular que el servicio de avisos cae (caos controlado)
```powershell
$env:Notifications__Mode = "AlwaysFail"
dotnet run --project src/Api
```
Las asignaciones siguen funcionando y los avisos quedan en `GET /api/v1/admin/dead-letters`. Modos: `Ok`, `AlwaysFail` y `FailTwiceThenOk`.

## Arquitectura
Monolito modular con tres capas y dependencias hacia el dominio: **Api / Infrastructure → Application → Domain**.

![C4 nivel 3](docs/actividad-4/c4-nivel3-componentes-a4.png)

| Carpeta | Contiene |
|---|---|
| `src/Domain` | Entidades, objetos de valor, invariantes y eventos |
| `src/Application` | Casos de uso, contratos y puertos |
| `src/Infrastructure` | SQLite, migraciones, notificaciones, reloj y telemetría |
| `src/Api` | Entrada HTTP, seguridad y raíz de composición |
| `tests/PixelRoute.Tests` | 55 pruebas, incluidas las de arquitectura |

## Decisiones (ADR)
| ADR | Decisión |
|---|---|
| [ADR-001](docs/actividad-2/06-adr-001-monolito-modular.md) | Monolito modular con puertos y adaptadores |
| [ADR-002](docs/actividad-3/adr/ADR-002-tres-capas-y-direccion-de-dependencias.md) | Tres capas con dependencias hacia el dominio |
| [ADR-003](docs/actividad-3/adr/ADR-003-agregado-delivery-e-invariantes.md) | Agregado `Delivery` que protege sus invariantes |
| [ADR-004](docs/actividad-3/adr/ADR-004-puertos-eventos-y-consistencia.md) | Puertos de salida, eventos y consistencia |
| [ADR-005](docs/actividad-4/adr/ADR-005-api-rest-y-vertical-slice.md) | API REST y vertical slice |
| [ADR-006](docs/actividad-4/adr/ADR-006-persistencia-migraciones-y-concurrencia.md) | SQLite, migraciones y concurrencia optimista |
| [ADR-007](docs/actividad-4/adr/ADR-007-notificaciones-resilientes-y-cola-de-fallidos.md) | Notificaciones con reintentos y cola de fallidos |
| [ADR-008](docs/actividad-4/adr/ADR-008-seguridad-observabilidad-y-verificacion.md) | Seguridad, observabilidad y verificación automática |

## Documentación
| Documento | Descripción |
|---|---|
| [Architecture.md](Architecture.md) | Expediente arquitectónico: C4, capas, decisiones y riesgos |
| [Actividad 4 · 00](docs/actividad-4/00-plan-y-trazabilidad.md) | Qué se implementó y trazabilidad con el diseño |
| [Actividad 4 · 01](docs/actividad-4/01-decisiones-de-implementacion.md) | Decisiones y qué patrones aplican y cuáles no |
| [Actividad 4 · 02](docs/actividad-4/02-riesgos-seguridad-y-operacion.md) | Riesgos operativos y de seguridad, con mitigaciones y evidencias |
| [Actividad 4 · 03](docs/actividad-4/03-observabilidad.md) | Señales de observabilidad y experimento de caos |
| [Actividad 4 · 04](docs/actividad-4/04-pruebas-y-resultados.md) | Pruebas, resultados, riesgos residuales y próximos pasos |
| [Actividad 4 · 05](docs/actividad-4/05-guion-video.md) | Guion del video |
| [Actividad 3](docs/actividad-3/) | Diseño y dominio |
| [Actividad 2](docs/actividad-2/) | Requisitos y decisión estructural |

## Integración continua
Cada `push` y cada Pull Request ejecutan `dotnet build` y `dotnet test` (incluidas las pruebas de arquitectura) en GitHub Actions: [`.github/workflows/ci.yml`](.github/workflows/ci.yml).

## Video de demostración y defensa
_(pegar aquí el enlace de YouTube, Google Drive o Teams cuando esté publicado)_

## Autor
Andrés Barrera Claros
