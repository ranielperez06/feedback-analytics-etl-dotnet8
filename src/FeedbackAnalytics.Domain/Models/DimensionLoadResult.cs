namespace FeedbackAnalytics.Domain.Models;

public sealed record DimensionLoadResult(
    int DateRows,
    int SourceRows,
    int AuthorRows,
    int ProductRows,
    int FactRows,
    TimeSpan Duration);
