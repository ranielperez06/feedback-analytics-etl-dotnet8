using FeedbackAnalytics.Domain.Models;

namespace FeedbackAnalytics.Domain.Contracts;

public interface IAnalyticsLoader
{
    Task<DimensionLoadResult> LoadAsync(CancellationToken cancellationToken);
}
