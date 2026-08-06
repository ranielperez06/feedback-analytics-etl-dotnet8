using System.Text.Json;
using System.Text.RegularExpressions;
using FeedbackAnalytics.Domain.Contracts;
using FeedbackAnalytics.Domain.Models;
using FeedbackAnalytics.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace FeedbackAnalytics.Infrastructure.Staging;

public sealed partial class PostgresStagingWriter : IStagingWriter
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly StagingOptions _options;
    private readonly ILogger<PostgresStagingWriter> _logger;

    public PostgresStagingWriter(
        NpgsqlDataSource dataSource,
        IOptions<StagingOptions> options,
        ILogger<PostgresStagingWriter> logger)
    {
        _dataSource = dataSource;
        _options = options.Value;
        _logger = logger;

        ValidateIdentifier(_options.Schema, nameof(_options.Schema));
        ValidateIdentifier(_options.Table, nameof(_options.Table));
    }

    public async Task<string> WriteAsync(
        string sourceName,
        IReadOnlyCollection<ExtractedRecord> records,
        CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection =
            await _dataSource.OpenConnectionAsync(cancellationToken);

        await EnsureStagingTableAsync(connection, cancellationToken);

        string copyCommand =
            $"""
             COPY "{_options.Schema}"."{_options.Table}"
             (batch_id, record_id, source_type, source_name, external_id, author_name,
              content, score, source_created_at, extracted_at, metadata)
             FROM STDIN (FORMAT BINARY)
             """;

        string batchId = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";

        await using NpgsqlBinaryImporter importer =
            await connection.BeginBinaryImportAsync(copyCommand, cancellationToken);

        foreach (ExtractedRecord record in records)
        {
            await importer.StartRowAsync(cancellationToken);
            await importer.WriteAsync(batchId, NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(record.Id, NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(
                record.SourceType.ToString(),
                NpgsqlDbType.Text,
                cancellationToken);
            await importer.WriteAsync(record.SourceName, NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(record.ExternalId, NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(record.Author, NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(record.Content, NpgsqlDbType.Text, cancellationToken);

            if (record.Score.HasValue)
            {
                await importer.WriteAsync(
                    record.Score.Value,
                    NpgsqlDbType.Numeric,
                    cancellationToken);
            }
            else
            {
                await importer.WriteNullAsync(cancellationToken);
            }

            await importer.WriteAsync(
                record.CreatedAtUtc,
                NpgsqlDbType.TimestampTz,
                cancellationToken);
            await importer.WriteAsync(
                DateTimeOffset.UtcNow,
                NpgsqlDbType.TimestampTz,
                cancellationToken);
            await importer.WriteAsync(
                JsonSerializer.Serialize(record.Metadata),
                NpgsqlDbType.Jsonb,
                cancellationToken);
        }

        ulong rowsWritten = await importer.CompleteAsync(cancellationToken);

        _logger.LogInformation(
            "Staging batch {BatchId} wrote {RowCount} rows for {SourceName}.",
            batchId,
            rowsWritten,
            sourceName);

        return $"postgresql://{_options.Schema}.{_options.Table}?batch={batchId}";
    }

    private async Task EnsureStagingTableAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        string sql =
            $"""
             CREATE SCHEMA IF NOT EXISTS "{_options.Schema}";

             CREATE TABLE IF NOT EXISTS "{_options.Schema}"."{_options.Table}" (
                 staging_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                 batch_id TEXT NOT NULL,
                 record_id TEXT NOT NULL,
                 source_type TEXT NOT NULL,
                 source_name TEXT NOT NULL,
                 external_id TEXT NOT NULL,
                 author_name TEXT NOT NULL,
                 content TEXT NOT NULL,
                 score NUMERIC(5,2) NULL,
                 source_created_at TIMESTAMPTZ NOT NULL,
                 extracted_at TIMESTAMPTZ NOT NULL,
                 metadata JSONB NOT NULL DEFAULT '{{}}'::jsonb
             );

             CREATE INDEX IF NOT EXISTS ix_{_options.Table}_batch
                 ON "{_options.Schema}"."{_options.Table}" (batch_id);

             CREATE INDEX IF NOT EXISTS ix_{_options.Table}_source
                 ON "{_options.Schema}"."{_options.Table}" (source_name, extracted_at);
             """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void ValidateIdentifier(string value, string parameterName)
    {
        if (!SafeIdentifierRegex().IsMatch(value))
        {
            throw new ArgumentException(
                "Only letters, numbers and underscores are accepted in PostgreSQL identifiers.",
                parameterName);
        }
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeIdentifierRegex();
}
