namespace FeedbackAnalytics.Infrastructure.Options;

public sealed class DatabaseSourceOptions
{
    public const string SectionName = "Sources:PostgreSql";

    public string Query { get; init; } =
        """
        SELECT review_id, author_name, review_text, score, created_at, product_name
        FROM source.reviews
        ORDER BY created_at
        """;
}
