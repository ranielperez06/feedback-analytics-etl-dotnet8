namespace FeedbackAnalytics.Domain.Models;

public sealed record ExtractionResult(
    string SourceName,
    bool IsSuccessful,
    IReadOnlyCollection<ExtractedRecord> Records,
    TimeSpan Duration,
    string? ErrorMessage = null);
