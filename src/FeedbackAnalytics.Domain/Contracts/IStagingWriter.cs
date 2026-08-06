using FeedbackAnalytics.Domain.Models;

namespace FeedbackAnalytics.Domain.Contracts;

public interface IStagingWriter
{
    Task<string> WriteAsync(
        string sourceName,
        IReadOnlyCollection<ExtractedRecord> records,
        CancellationToken cancellationToken);
}
