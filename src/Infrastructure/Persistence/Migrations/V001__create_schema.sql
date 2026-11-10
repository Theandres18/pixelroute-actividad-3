-- V001: esquema inicial de PixelRoute.
-- Las reglas más importantes se repiten como restricciones de la base de datos (defensa en profundidad).

CREATE TABLE couriers (
    id                    TEXT    NOT NULL PRIMARY KEY,
    name                  TEXT    NOT NULL,
    is_online             INTEGER NOT NULL CHECK (is_online IN (0, 1)),
    max_active_deliveries INTEGER NOT NULL CHECK (max_active_deliveries > 0)
);

CREATE TABLE deliveries (
    id          TEXT    NOT NULL PRIMARY KEY,
    order_id    TEXT    NOT NULL,
    address     TEXT    NOT NULL CHECK (length(trim(address)) > 0),
    promised_by TEXT    NOT NULL,
    status      TEXT    NOT NULL CHECK (status IN ('Pending', 'Assigned', 'InRoute', 'Delivered', 'Failed', 'Cancelled')),
    courier_id  TEXT    NULL,
    assigned_at TEXT    NULL,
    version     INTEGER NOT NULL CHECK (version >= 1),
    -- INV-03: una entrega Pendiente o Cancelada no tiene repartidor; una Asignada o En ruta siempre lo tiene.
    CHECK (status NOT IN ('Pending', 'Cancelled') OR courier_id IS NULL),
    CHECK (status NOT IN ('Assigned', 'InRoute') OR courier_id IS NOT NULL)
);

CREATE INDEX ix_deliveries_courier_status ON deliveries (courier_id, status);

-- Cola de mensajes fallidos: avisos que no se pudieron entregar tras agotar los reintentos.
CREATE TABLE dead_letter_messages (
    id           TEXT    NOT NULL PRIMARY KEY,
    message_type TEXT    NOT NULL,
    payload      TEXT    NOT NULL,
    error        TEXT    NOT NULL,
    attempts     INTEGER NOT NULL,
    created_at   TEXT    NOT NULL
);
