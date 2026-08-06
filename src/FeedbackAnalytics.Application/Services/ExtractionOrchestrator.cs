using System.Collections.Concurrent;
using System.Diagnostics;
using FeedbackAnalytics.Domain.Contracts;
using FeedbackAnalytics.Domain.Models;
using Microsoft.Extensions.Logging;

namespace FeedbackAnalytics.Application.Services;

public sealed class ExtractionOrchestrator(
    IEnumerable<IExtractor> extractors,
    IStagingWriter stagingWriter,
    ILogger<ExtractionOrchestrator> logger)
{
    private readonly IReadOnlyCollection<IExtractor> _extractors = extractors.ToArray();

    public async Task<ExtractionSummary> RunAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset startedAtUtc = DateTimeOffset.UtcNow;
        logger.LogInformation(
            "ETL extraction started at {StartedAtUtc} with {SourceCount} configured sources.",
            startedAtUtc,
            _extractors.Count);

        var results = new ConcurrentBag<ExtractionResult>();

        await Parallel.ForEachAsync(
            _extractors,
            cancellationToken,
            async (extractor, token) =>
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                try
                {
                    IReadOnlyCollection<ExtractedRecord> records =
                        await extractor.ExtractAsync(token);

                    string stagingPath =
                        await stagingWriter.WriteAsync(extractor.SourceName, records, token);

                    stopwatch.Stop();
                    results.Add(
                        new ExtractionResult(
                            extractor.SourceName,
                            true,
                            records,
                            stopwatch.Elapsed));

                    logger.LogInformation(
                        "Source {SourceName} extracted {RecordCount} records in {ElapsedMs} ms. Staging: {StagingPath}",
                        extractor.SourceName,
                        records.Count,
                        stopwatch.ElapsedMilliseconds,
                        stagingPath);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    stopwatch.Stop();
                    results.Add(
                        new ExtractionResult(
                            extractor.SourceName,
                            false,
                            Array.Empty<ExtractedRecord>(),
                            stopwatch.Elapsed,
                            exception.Message));

                    logger.LogError(
                        exception,
                        "Extraction failed for source {SourceName} after {ElapsedMs} ms.",
                        extractor.SourceName,
                        stopwatch.ElapsedMilliseconds);
                }
            });

        DateTimeOffset finishedAtUtc = DateTimeOffset.UtcNow;
        ExtractionSummary summary = new(
            startedAtUtc,
            finishedAtUtc,
            results.OrderBy(result => result.SourceName).ToArray());

        logger.LogInformation(
            "ETL extraction finished. Success: {Success}. Records: {RecordCount}. Duration: {ElapsedMs} ms.",
            summary.IsSuccessful,
            summary.TotalRecords,
            (finishedAtUtc - startedAtUtc).TotalMilliseconds);

        return summary;
    }
}
