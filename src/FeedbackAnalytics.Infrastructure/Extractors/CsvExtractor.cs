using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using FeedbackAnalytics.Domain.Contracts;
using FeedbackAnalytics.Domain.Enums;
using FeedbackAnalytics.Domain.Models;
using FeedbackAnalytics.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FeedbackAnalytics.Infrastructure.Extractors;

public sealed class CsvExtractor(
    IOptions<CsvSourceOptions> options,
    ILogger<CsvExtractor> logger) : IExtractor
{
    private readonly CsvSourceOptions _options = options.Value;

    public string SourceName => "InternalSurveyCsv";

    public async Task<IReadOnlyCollection<ExtractedRecord>> ExtractAsync(
        CancellationToken cancellationToken)
    {
        string fullPath = Path.GetFullPath(_options.FilePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The configured survey CSV was not found.", fullPath);
        }

        logger.LogInformation("Reading survey data from {CsvPath}.", fullPath);

        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = _options.Delimiter,
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            HeaderValidated = null
        };

        using var streamReader = new StreamReader(fullPath);
        using var csvReader = new CsvReader(streamReader, configuration);
        var records = new List<ExtractedRecord>();

        await foreach (SurveyCsvRow row in
                       csvReader.GetRecordsAsync<SurveyCsvRow>(cancellationToken))
        {
            records.Add(
                new ExtractedRecord(
                    Guid.NewGuid().ToString("N"),
                    DataSourceType.Csv,
                    SourceName,
                    row.SurveyId,
                    row.Participant,
                    row.Comment,
                    row.Score,
                    row.CreatedAt.ToUniversalTime(),
                    new Dictionary<string, string>
                    {
                        ["area"] = row.Area,
                        ["channel"] = row.Channel
                    }));
        }

        return records;
    }

    private sealed class SurveyCsvRow
    {
        [Name("survey_id")]
        public string SurveyId { get; init; } = string.Empty;

        [Name("participant")]
        public string Participant { get; init; } = string.Empty;

        [Name("comment")]
        public string Comment { get; init; } = string.Empty;

        [Name("score")]
        public decimal Score { get; init; }

        [Name("created_at")]
        public DateTimeOffset CreatedAt { get; init; }

        [Name("area")]
        public string Area { get; init; } = string.Empty;

        [Name("channel")]
        public string Channel { get; init; } = string.Empty;
    }
}
