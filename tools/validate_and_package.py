from __future__ import annotations

import csv
import hashlib
import json
import zipfile
from pathlib import Path
from xml.etree import ElementTree

from PIL import Image
from pypdf import PdfReader


ROOT = Path(__file__).resolve().parent.parent
DELIVERY = ROOT / "entrega"
REPORT = DELIVERY / "REPORTE_VALIDACION.txt"
MANIFEST = DELIVERY / "MANIFEST_ENTREGA.txt"
PACKAGE = DELIVERY / "ENTREGA_Actividad1_ETL_NET8.zip"


class Validation:
    def __init__(self) -> None:
        self.results: list[tuple[str, bool, str]] = []

    def check(self, name: str, condition: bool, detail: str) -> None:
        self.results.append((name, condition, detail))
        if not condition:
            raise AssertionError(f"{name}: {detail}")

    def render_report(self) -> str:
        lines = [
            "REPORTE DE VALIDACIÓN - ETL Y CARGA DIMENSIONAL .NET 8",
            "=" * 54,
            "",
        ]
        for name, passed, detail in self.results:
            status = "PASS" if passed else "FAIL"
            lines.append(f"[{status}] {name}: {detail}")

        lines.extend(
            [
                "",
                "LIMITACIÓN DEL ENTORNO",
                "El equipo de preparación contiene el runtime .NET 8 pero no el SDK.",
                "PostgreSQL está activo, pero no se usaron credenciales personales.",
                "Por esas razones, la compilación y la ejecución autenticada se realizan",
                "con scripts reproducibles en el entorno local del estudiante.",
                "",
                "RESULTADO",
                "La estructura, configuración, código, SQL, modelo dimensional,",
                "diagramas, documento y ZIP cumplen las verificaciones definidas.",
            ]
        )
        return "\n".join(lines) + "\n"


def validate_projects(validation: Validation) -> None:
    expected_projects = [
        ROOT / "src/FeedbackAnalytics.Domain/FeedbackAnalytics.Domain.csproj",
        ROOT / "src/FeedbackAnalytics.Application/FeedbackAnalytics.Application.csproj",
        ROOT / "src/FeedbackAnalytics.Infrastructure/FeedbackAnalytics.Infrastructure.csproj",
        ROOT / "src/FeedbackAnalytics.Worker/FeedbackAnalytics.Worker.csproj",
    ]
    validation.check(
        "Proyectos por capas",
        all(path.exists() for path in expected_projects),
        "Domain, Application, Infrastructure y Worker presentes",
    )

    for project in expected_projects:
        ElementTree.parse(project)

    props = ElementTree.parse(ROOT / "Directory.Build.props")
    target = props.find(".//TargetFramework")
    validation.check(
        ".NET 8",
        target is not None and target.text == "net8.0",
        "TargetFramework net8.0 configurado",
    )

    infrastructure_xml = (
        ROOT / "src/FeedbackAnalytics.Infrastructure/FeedbackAnalytics.Infrastructure.csproj"
    ).read_text(encoding="utf-8")
    validation.check(
        "Paquetes de extracción",
        "CsvHelper" in infrastructure_xml and "Npgsql" in infrastructure_xml,
        "CsvHelper y Npgsql referenciados",
    )


def validate_code(validation: Validation) -> None:
    code_expectations = {
        "CSV": (
            ROOT
            / "src/FeedbackAnalytics.Infrastructure/Extractors/CsvExtractor.cs",
            ["CsvReader", "GetRecordsAsync", "DataSourceType.Csv"],
        ),
        "PostgreSQL": (
            ROOT
            / "src/FeedbackAnalytics.Infrastructure/Extractors/DatabaseExtractor.cs",
            ["NpgsqlDataSource", "ExecuteReaderAsync", "DataSourceType.RelationalDatabase"],
        ),
        "API REST": (
            ROOT
            / "src/FeedbackAnalytics.Infrastructure/Extractors/ApiExtractor.cs",
            ["HttpClient", "GetFromJsonAsync", "DataSourceType.RestApi"],
        ),
        "Staging PostgreSQL": (
            ROOT
            / "src/FeedbackAnalytics.Infrastructure/Staging/PostgresStagingWriter.cs",
            ["BeginBinaryImportAsync", "FORMAT BINARY", "Jsonb"],
        ),
        "Orquestación": (
            ROOT
            / "src/FeedbackAnalytics.Application/Services/ExtractionOrchestrator.cs",
            ["Parallel.ForEachAsync", "Stopwatch", "LogError"],
        ),
        "Carga dimensional": (
            ROOT
            / "src/FeedbackAnalytics.Infrastructure/Analytics/PostgresDimensionLoader.cs",
            [
                "IAnalyticsLoader",
                "BeginTransactionAsync",
                "ON CONFLICT",
                "dim_date",
                "dim_source",
                "dim_author",
                "dim_product",
                "feedback_fact",
                "CommitAsync",
                "RollbackAsync",
            ],
        ),
        "Ejecución del Warehouse": (
            ROOT / "src/FeedbackAnalytics.Worker/Worker.cs",
            ["IAnalyticsLoader", "LoadAsync", "Warehouse ready"],
        ),
    }

    for name, (path, tokens) in code_expectations.items():
        text = path.read_text(encoding="utf-8")
        validation.check(
            f"Código {name}",
            all(token in text for token in tokens),
            f"{path.name}: contratos y operaciones requeridas presentes",
        )

    appsettings_path = ROOT / "src/FeedbackAnalytics.Worker/appsettings.json"
    appsettings = json.loads(appsettings_path.read_text(encoding="utf-8"))
    validation.check(
        "Credenciales seguras",
        appsettings["ConnectionStrings"]["PostgreSql"] is None,
        "appsettings.json no almacena usuario ni contraseña",
    )


def validate_data_and_sql(validation: Validation) -> None:
    csv_path = ROOT / "data/input/encuestas.csv"
    with csv_path.open(encoding="utf-8", newline="") as stream:
        rows = list(csv.DictReader(stream))
    validation.check(
        "Datos CSV",
        len(rows) == 5
        and set(rows[0])
        == {
            "survey_id",
            "participant",
            "comment",
            "score",
            "created_at",
            "area",
            "channel",
        },
        "5 encuestas con el esquema esperado",
    )

    sql = (ROOT / "database/001_create_database_objects.sql").read_text(
        encoding="utf-8"
    )
    validation.check(
        "Objetos PostgreSQL",
        all(
            token in sql
            for token in [
                "source.reviews",
                "staging.extracted_feedback",
                "analytics.feedback_fact",
                "JSONB",
            ]
        ),
        "source, staging y analytics definidos",
    )

    dimensional_sql = (
        ROOT / "database/004_create_and_load_dimensions.sql"
    ).read_text(encoding="utf-8")
    validation_sql = (
        ROOT / "database/005_validate_dimensional_load.sql"
    ).read_text(encoding="utf-8")
    validation.check(
        "Modelo dimensional",
        all(
            token in dimensional_sql
            for token in [
                "analytics.dim_date",
                "analytics.dim_source",
                "analytics.dim_author",
                "analytics.dim_product",
                "ON CONFLICT",
                "VALIDATE CONSTRAINT",
                "COMMIT",
            ]
        ),
        "cuatro dimensiones, carga idempotente e integridad referencial",
    )
    validation.check(
        "Consultas de validación",
        "orphan_facts" in validation_sql
        and "duplicate_count" in validation_sql
        and "JOIN analytics.dim_date" in validation_sql,
        "conteos, consulta estrella, huérfanos y duplicados",
    )


def validate_visuals(validation: Validation) -> None:
    for name in [
        "Diagrama_Arquitectura.png",
        "Diagrama_Flujo_ETL.png",
        "Modelo_Dimensional.png",
        "Flujo_Carga_Dimensiones.png",
    ]:
        path = ROOT / "docs" / name
        with Image.open(path) as image:
            validation.check(
                f"Diagrama {name}",
                image.width >= 1200 and image.height >= 900,
                f"{image.width}x{image.height} píxeles",
            )

    pdf_path = DELIVERY / "Documento_Tecnico_Actividad1_ETL_NET8.pdf"
    reader = PdfReader(pdf_path)
    text = "\n".join(page.extract_text() or "" for page in reader.pages)
    expected = [
        "Arquitectura dimensional",
        "Descripción de las dimensiones",
        "Tabla de hechos",
        "Proceso de carga",
        "Evidencia de implementación",
        "Validación del funcionamiento",
        "Conclusión",
        "github.com/ranielperez06/feedback-analytics-etl-dotnet8",
    ]
    validation.check(
        "Documento técnico PDF",
        8 <= len(reader.pages) <= 12 and all(value in text for value in expected),
        f"{len(reader.pages)} páginas con modelo, tablas, proceso, código y validación",
    )


def should_package(path: Path) -> bool:
    relative = path.relative_to(ROOT)
    parts = set(relative.parts)
    if parts.intersection({".git", "bin", "obj", "tmp"}):
        return False
    if path == PACKAGE:
        return False
    if path.suffix in {".pyc", ".db", ".db-shm", ".db-wal"}:
        return False
    return True


def create_manifest_and_package(
    validation: Validation,
    *,
    record_result: bool,
) -> None:
    files = sorted(
        path
        for path in ROOT.rglob("*")
        if path.is_file() and should_package(path) and path != MANIFEST
    )

    manifest_lines = []
    for path in files:
        relative = path.relative_to(ROOT).as_posix()
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        manifest_lines.append(f"{digest}  {relative}")
    MANIFEST.write_text("\n".join(manifest_lines) + "\n", encoding="utf-8")

    files = sorted(
        path
        for path in ROOT.rglob("*")
        if path.is_file() and should_package(path)
    )

    with zipfile.ZipFile(PACKAGE, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for path in files:
            relative = Path("Actividad1_ETL_NET8") / path.relative_to(ROOT)
            archive.write(path, relative.as_posix())

    with zipfile.ZipFile(PACKAGE) as archive:
        bad_file = archive.testzip()
        entry_count = len(archive.infolist())

    if record_result:
        validation.check(
            "Paquete ZIP",
            bad_file is None and entry_count >= 35,
            f"CRC correcto y {entry_count} archivos incluidos",
        )
    elif bad_file is not None:
        raise AssertionError(f"CRC failure in packaged file: {bad_file}")


def main() -> None:
    DELIVERY.mkdir(exist_ok=True)
    validation = Validation()
    validate_projects(validation)
    validate_code(validation)
    validate_data_and_sql(validation)
    validate_visuals(validation)

    REPORT.write_text(validation.render_report(), encoding="utf-8")
    create_manifest_and_package(validation, record_result=True)
    REPORT.write_text(validation.render_report(), encoding="utf-8")

    # Rebuild so the package contains the final validation report and manifest.
    create_manifest_and_package(validation, record_result=False)

    print(validation.render_report())
    print(f"package={PACKAGE}")


if __name__ == "__main__":
    main()
