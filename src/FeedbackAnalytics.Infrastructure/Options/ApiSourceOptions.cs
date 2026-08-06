namespace FeedbackAnalytics.Infrastructure.Options;

public sealed class ApiSourceOptions
{
    public const string SectionName = "Sources:Api";

    public string BaseUrl { get; init; } = "https://jsonplaceholder.typicode.com/";

    public string Endpoint { get; init; } = "comments?postId=1";

    public int MaxRecords { get; init; } = 25;

    public int TimeoutSeconds { get; init; } = 30;
}
