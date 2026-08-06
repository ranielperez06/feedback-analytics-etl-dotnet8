namespace FeedbackAnalytics.Infrastructure.Options;

public sealed class StagingOptions
{
    public const string SectionName = "Staging";

    public string Schema { get; init; } = "staging";

    public string Table { get; init; } = "extracted_feedback";
}
