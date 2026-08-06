using FeedbackAnalytics.Domain.Models;

namespace FeedbackAnalytics.Domain.Contracts;

public interface IExtractor
{
    string SourceName { get; }

    Task<IReadOnlyCollection<ExtractedRecord>> ExtractAsync(
        CancellationToken cancellationToken);
}
