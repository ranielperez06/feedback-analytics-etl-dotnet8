# Feedback Analytics ETL

Worker Service en .NET 8 para extraer comentarios y reseñas desde tres fuentes:

1. Encuestas internas en CSV.
2. Reseñas almacenadas en PostgreSQL.
3. Comentarios obtenidos desde una API REST.

Los datos se normalizan a un modelo común y se guardan en
`staging.extracted_feedback` dentro de PostgreSQL. Esta entrega implementa la
fase **E (Extract)** del proceso ETL y deja preparada la base analítica para las
fases posteriores.

## Requisitos

- .NET SDK 8.
- PostgreSQL 14 o superior.
- Acceso a Internet para restaurar paquetes NuGet y consumir la API de ejemplo.

Comprueba el SDK con:

```powershell
dotnet --list-sdks
```

## Arquitectura

La solución aplica separación de responsabilidades mediante cuatro proyectos:

- `FeedbackAnalytics.Domain`: modelos e interfaces sin dependencias externas.
- `FeedbackAnalytics.Application`: coordinación de las extracciones.
- `FeedbackAnalytics.Infrastructure`: CSV, PostgreSQL, API y staging.
- `FeedbackAnalytics.Worker`: proceso en segundo plano, configuración y logs.

La interfaz `IExtractor` permite añadir una nueva fuente sin modificar el
orquestador. Cada extractor se ejecuta en paralelo y utiliza APIs asíncronas.

## Preparación de PostgreSQL

1. Crea una base de datos llamada `feedback_analytics`.
2. Ejecuta, en este orden:

```text
database/001_create_database_objects.sql
database/002_seed_source_reviews.sql
database/003_least_privilege.sql
```

El tercer script supone que existe un usuario llamado `etl_user`. Crea ese
usuario con una contraseña segura y conserva la contraseña fuera del
repositorio.

## Configuración segura

`appsettings.json` no contiene contraseñas. Configura la conexión mediante una
variable de entorno:

```powershell
.\scripts\configure-local.ps1 `
    -HostName localhost `
    -Database feedback_analytics `
    -Username etl_user `
    -Password (Read-Host "Contraseña" -AsSecureString)
```

La variable creada se llama `ConnectionStrings__PostgreSql` y solo permanece
en la sesión actual de PowerShell.

## Ejecución

Desde la raíz del proyecto:

```powershell
.\scripts\run-etl.ps1
```

El script restaura los paquetes, compila en modo Release y ejecuta el Worker.
Al finalizar, consulta los resultados con:

```sql
SELECT
    source_name,
    COUNT(*) AS records,
    MIN(extracted_at) AS first_extraction,
    MAX(extracted_at) AS last_extraction
FROM staging.extracted_feedback
GROUP BY source_name
ORDER BY source_name;
```

## Atributos de calidad

### Rendimiento

- `Parallel.ForEachAsync` ejecuta las fuentes en paralelo.
- Todas las operaciones de archivos, HTTP y PostgreSQL son asíncronas.
- La escritura staging usa `COPY ... FORMAT BINARY` de Npgsql.
- `Stopwatch` mide el tiempo de cada extractor.

### Escalabilidad

- Cada fuente implementa `IExtractor`.
- Las fuentes se registran mediante inyección de dependencias.
- Una nueva fuente no requiere modificar `ExtractionOrchestrator`.
- PostgreSQL puede escalar verticalmente, usar réplicas y particionar staging.

### Seguridad

- Las credenciales se reciben mediante variables de entorno.
- `.gitignore` excluye configuraciones locales y bases temporales.
- El script SQL aplica privilegios mínimos al usuario ETL.
- Los identificadores de esquema y tabla se validan antes de construir SQL.

### Mantenibilidad

- Capas independientes y referencias en una sola dirección.
- Clases pequeñas con una responsabilidad clara.
- Opciones centralizadas en `appsettings.json`.
- Logs estructurados con `ILogger`.

## Evidencias y documentación

- `docs/Diagrama_Arquitectura.png`
- `docs/Diagrama_Flujo_ETL.png`
- El documento técnico y el ZIP final se entregan por separado en el aula
  virtual para no publicar datos académicos personales.

## Publicación en GitHub

Consulta `GUIA_PUBLICACION_GITHUB.md` para conocer el flujo utilizado.

## Nota de validación

El código fue revisado estructuralmente contra los requisitos. El equipo usado
para preparar la entrega tiene el runtime .NET 8, pero no el SDK, por lo que la
compilación final debe realizarse en un equipo que tenga instalado el SDK 8.
