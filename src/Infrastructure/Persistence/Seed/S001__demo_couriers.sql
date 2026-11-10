-- Datos de demostración. Solo se aplican cuando Database:SeedDemoData es true (nunca en producción).
INSERT OR IGNORE INTO couriers (id, name, is_online, max_active_deliveries) VALUES
    ('11111111-1111-1111-1111-111111111111', 'Repartidor demo 1 (en línea, máximo 3)', 1, 3),
    ('22222222-2222-2222-2222-222222222222', 'Repartidor demo 2 (fuera de línea)', 0, 3),
    ('33333333-3333-3333-3333-333333333333', 'Repartidor demo 3 (en línea, máximo 1)', 1, 1);
