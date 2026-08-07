# Matriz de cumplimiento

| Requisito | Implementación | Evidencia |
|---|---|---|
| Worker Service .NET 8 | Proyecto `FeedbackAnalytics.Worker` | `src/FeedbackAnalytics.Worker` |
| CSV | `CsvExtractor` con CsvHelper | `Extractors/CsvExtractor.cs` |
| Base relacional | `DatabaseExtractor` con Npgsql | `Extractors/DatabaseExtractor.cs` |
| API REST | `ApiExtractor` con `HttpClient` | `Extractors/ApiExtractor.cs` |
| Staging | Tabla PostgreSQL y carga binaria | `PostgresStagingWriter.cs` |
| Dimensión fecha | Carga idempotente con clave YYYYMMDD | `analytics.dim_date` |
| Dimensión fuente | Tipo y nombre de cada origen | `analytics.dim_source` |
| Dimensión autor | Participantes y autores sin duplicados | `analytics.dim_author` |
| Dimensión producto | Producto, área o publicación relacionada | `analytics.dim_product` |
| Tabla de hechos | Cuatro claves foráneas y granularidad por comentario | `analytics.feedback_fact` |
| Integridad | Claves foráneas, índices únicos y validación de huérfanos | scripts `004` y `005` |
| Idempotencia | `ON CONFLICT` actualiza sin duplicar | `PostgresDimensionLoader.cs` |
| Logs | Logs estructurados | `ILogger` en todos los componentes |
| Rendimiento | Paralelismo, asincronía y `Stopwatch` | `ExtractionOrchestrator.cs` |
| Escalabilidad | Interfaz común y DI | `IExtractor.cs` |
| Seguridad | Variables de entorno y privilegio mínimo | scripts y `.gitignore` |
| Mantenibilidad | Domain, Application, Infrastructure y Worker | solución completa |
| Diagrama de arquitectura | PNG y SVG | carpeta `docs` |
| Diagrama de flujo | PNG y SVG | carpeta `docs` |
| Documento técnico | PDF actualizado y verificado visualmente | archivo local de entrega |
| GitHub | Repositorio público con el código fuente | enlace incluido en la entrega |
