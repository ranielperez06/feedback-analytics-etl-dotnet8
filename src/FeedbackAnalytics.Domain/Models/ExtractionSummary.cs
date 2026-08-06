namespace FeedbackAnalytics.Domain.Models;

public sealed record ExtractionSummary(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc,
    IReadOnlyCollection<ExtractionResult> Results)
{
    public int TotalRecords => Results.Sum(result => result.Records.Count);

    public bool IsSuccessful => Results.All(result => result.IsSuccessful);
}
