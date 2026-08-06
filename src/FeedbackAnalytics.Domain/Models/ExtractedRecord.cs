using FeedbackAnalytics.Domain.Enums;

namespace FeedbackAnalytics.Domain.Models;

public sealed record ExtractedRecord(
    string Id,
    DataSourceType SourceType,
    string SourceName,
    string ExternalId,
    string Author,
    string Content,
    decimal? Score,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyDictionary<string, string> Metadata);
