# Feedback Analytics ETL

Worker Service en .NET 8 para extraer comentarios y reseñas desde tres fuentes,
guardarlos en staging y cargar un Data Warehouse dimensional:

1. Encuestas internas en CSV.
2. Reseñas almacenadas en PostgreSQL.
3. Comentarios obtenidos desde una API REST.

Los datos se normalizan en `staging.extracted_feedback`. Después, el
`PostgresDimensionLoader` ejecuta las fases **T (Transform)** y **L (Load)**:
carga cuatro dimensiones y relaciona cada comentario con
`analytics.feedback_fact`.

## Modelo dimensional

La solución utiliza un esquema estrella:

- `analytics.dim_date`: fecha, día, mes, trimestre, año y fin de semana.
- `analytics.dim_source`: nombre y tipo de la fuente de origen.
- `analytics.dim_author`: autor o participante del comentario.
- `analytics.dim_product`: producto, área o publicación relacionada.
- `analytics.feedback_fact`: comentario, puntuación y claves foráneas.

Las claves de las dimensiones son sustitutas. La fecha utiliza una clave
entera `YYYYMMDD`; las demás dimensiones usan columnas `IDENTITY`. La carga
aplica `ON CONFLICT`, por lo que es idempotente y no duplica dimensiones ni
hechos cuando se repite.

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

El Worker crea y carga las dimensiones automáticamente después de una
extracción correcta. Como alternativa de administración, puedes ejecutar
`database/004_create_and_load_dimensions.sql` después de llenar staging.

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
El flujo completo es:

```text
CSV + PostgreSQL + API
          |
          v
staging.extracted_feedback
          |
          v
dim_date + dim_source + dim_author + dim_product
          |
          v
analytics.feedback_fact
```

Al finalizar, ejecuta `database/005_validate_dimensional_load.sql`. La consulta
principal relaciona las cinco tablas y las comprobaciones adicionales deben
producir cero hechos huérfanos y cero duplicados.

```sql
SELECT
    source_name,
    COUNT(*) AS records,
    MIN(extracted_at) AS first_extraction,
    MAX(extracted_at) AS last_extraction
FROM analytics.feedback_fact AS fact
JOIN analytics.dim_source AS source_dim
    ON source_dim.source_key = fact.source_key
GROUP BY source_dim.source_name
ORDER BY source_dim.source_name;
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
- Las dimensiones pueden ampliarse sin cambiar los extractores.

### Seguridad

- Las credenciales se reciben mediante variables de entorno.
- `.gitignore` excluye configuraciones locales y bases temporales.
- El script SQL aplica privilegios mínimos a source, staging y analytics.
- Los identificadores de esquema y tabla se validan antes de construir SQL.

### Mantenibilidad

- Capas independientes y referencias en una sola dirección.
- Clases pequeñas con una responsabilidad clara.
- Opciones centralizadas en `appsettings.json`.
- Logs estructurados con `ILogger`.
- La carga dimensional se ejecuta dentro de una transacción.
- Los scripts y el cargador son idempotentes.

## Evidencias y documentación

- `docs/Diagrama_Arquitectura.png`
- `docs/Diagrama_Flujo_ETL.png`
- `database/004_create_and_load_dimensions.sql`
- `database/005_validate_dimensional_load.sql`
- El documento técnico y el ZIP final se entregan por separado en el aula
  virtual para no publicar datos académicos personales.

## Publicación en GitHub

Consulta `GUIA_PUBLICACION_GITHUB.md` para conocer el flujo utilizado.

## Nota de validación

El código y los scripts incluyen validaciones automatizadas de estructura,
integridad documental e idempotencia. Para la ejecución completa se requiere
el SDK de .NET 8 y una conexión autenticada a PostgreSQL.
