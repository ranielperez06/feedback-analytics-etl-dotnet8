# Matriz de cumplimiento

| Requisito | Implementación | Evidencia |
|---|---|---|
| Worker Service .NET 8 | Proyecto `FeedbackAnalytics.Worker` | `src/FeedbackAnalytics.Worker` |
| CSV | `CsvExtractor` con CsvHelper | `Extractors/CsvExtractor.cs` |
| Base relacional | `DatabaseExtractor` con Npgsql | `Extractors/DatabaseExtractor.cs` |
| API REST | `ApiExtractor` con `HttpClient` | `Extractors/ApiExtractor.cs` |
| Staging | Tabla PostgreSQL y carga binaria | `PostgresStagingWriter.cs` |
| Logs | Logs estructurados | `ILogger` en todos los componentes |
| Rendimiento | Paralelismo, asincronía y `Stopwatch` | `ExtractionOrchestrator.cs` |
| Escalabilidad | Interfaz común y DI | `IExtractor.cs` |
| Seguridad | Variables de entorno y privilegio mínimo | scripts y `.gitignore` |
| Mantenibilidad | Domain, Application, Infrastructure y Worker | solución completa |
| Diagrama de arquitectura | PNG y SVG | carpeta `docs` |
| Diagrama de flujo | PNG y SVG | carpeta `docs` |
| Documento técnico | PDF verificado y entregado por separado | archivo local de entrega |
| GitHub | Repositorio público con el código fuente | enlace indicado en la entrega |
