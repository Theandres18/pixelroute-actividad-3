# 05. Guion del video de demostración y defensa final (10 a 15 minutos)

| Minuto | Sección | Qué mostrar |
|---|---|---|
| 0:00 a 1:00 | Problema y decisión arquitectónica | README y Architecture.md: problema de PixelPack, monolito modular (ADR-001) |
| 1:00 a 2:30 | Modelo de dominio | Documento 02 de la Actividad 3 y `Delivery.cs`: entidad, objetos de valor e invariantes |
| 2:30 a 4:00 | Recorrido por las capas | Diagrama C4 nivel 3 de la Actividad 4 y la estructura de `src` |
| 4:00 a 7:30 | **Demostración del flujo** | API corriendo y `scripts/demo.ps1`: camino correcto, y errores y límites (409, 422, 404, 401) |
| 7:30 a 9:00 | Pruebas | `dotnet test`: capas de pruebas, un caso de error y un caso de límite, y la ejecución en GitHub Actions |
| 9:00 a 10:30 | Adaptadores, persistencia e integración | Migraciones, SQLite, control de concurrencia y notificaciones con reintentos y cola de fallidos (ADR-006 y ADR-007) |
| 10:30 a 12:00 | Riesgos, seguridad y observabilidad | Documentos 02 y 03: pre-mortem, mitigaciones, señales, caos controlado |
| 12:00 a 14:00 | Trade-offs, riesgos residuales y evolución | Decisiones (ADR-005 a ADR-008), riesgos residuales y próximos pasos |
| 14:00 a 14:30 | Cierre | Commits y Pull Request |
