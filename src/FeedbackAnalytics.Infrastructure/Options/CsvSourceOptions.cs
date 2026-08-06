namespace FeedbackAnalytics.Infrastructure.Options;

public sealed class CsvSourceOptions
{
    public const string SectionName = "Sources:Csv";

    public string FilePath { get; init; } = "data/input/encuestas.csv";

    public string Delimiter { get; init; } = ",";
}
