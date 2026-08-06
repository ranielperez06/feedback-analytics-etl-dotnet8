using System.Globalization;
using System.Net.Http.Json;
using FeedbackAnalytics.Domain.Contracts;
using FeedbackAnalytics.Domain.Enums;
using FeedbackAnalytics.Domain.Models;
using FeedbackAnalytics.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FeedbackAnalytics.Infrastructure.Extractors;

public sealed class ApiExtractor(
    HttpClient httpClient,
    IOptions<ApiSourceOptions> options,
    ILogger<ApiExtractor> logger) : IExtractor
{
    private readonly ApiSourceOptions _options = options.Value;

    public string SourceName => "RestApiComments";

    public async Task<IReadOnlyCollection<ExtractedRecord>> ExtractAsync(
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Requesting comments from endpoint {Endpoint}.", _options.Endpoint);

        ApiComment[] comments =
            await httpClient.GetFromJsonAsync<ApiComment[]>(
                _options.Endpoint,
                cancellationToken) ?? Array.Empty<ApiComment>();

        return comments
            .Take(_options.MaxRecords)
            .Select(
                comment =>
                    new ExtractedRecord(
                        Guid.NewGuid().ToString("N"),
                        DataSourceType.RestApi,
                        SourceName,
                        comment.Id.ToString(CultureInfo.InvariantCulture),
                        comment.Name,
                        comment.Body,
                        null,
                        DateTimeOffset.UtcNow,
                        new Dictionary<string, string>
                        {
                            ["postId"] = comment.PostId.ToString(CultureInfo.InvariantCulture),
                            ["contact"] = comment.Email
                        }))
            .ToArray();
    }

    private sealed record ApiComment(
        int PostId,
        int Id,
        string Name,
        string Email,
        string Body);
}
