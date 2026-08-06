using FeedbackAnalytics.Domain.Contracts;
using FeedbackAnalytics.Domain.Enums;
using FeedbackAnalytics.Domain.Models;
using FeedbackAnalytics.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FeedbackAnalytics.Infrastructure.Extractors;

public sealed class DatabaseExtractor(
    NpgsqlDataSource dataSource,
    IOptions<DatabaseSourceOptions> options,
    ILogger<DatabaseExtractor> logger) : IExtractor
{
    private readonly DatabaseSourceOptions _options = options.Value;

    public string SourceName => "PostgreSqlReviews";

    public async Task<IReadOnlyCollection<ExtractedRecord>> ExtractAsync(
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Querying relational reviews from PostgreSQL.");

        await using NpgsqlConnection connection =
            await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(_options.Query, connection);
        await using NpgsqlDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var records = new List<ExtractedRecord>();

        while (await reader.ReadAsync(cancellationToken))
        {
            DateTime createdAt = reader.GetDateTime(4);

            records.Add(
                new ExtractedRecord(
                    Guid.NewGuid().ToString("N"),
                    DataSourceType.RelationalDatabase,
                    SourceName,
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetDecimal(3),
                    new DateTimeOffset(createdAt.ToUniversalTime()),
                    new Dictionary<string, string>
                    {
                        ["product"] = reader.GetString(5)
                    }));
        }

        return records;
    }
}
