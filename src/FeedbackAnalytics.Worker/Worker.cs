using FeedbackAnalytics.Application.Services;
using FeedbackAnalytics.Domain.Models;

namespace FeedbackAnalytics.Worker;

public sealed class Worker(
    ExtractionOrchestrator orchestrator,
    IHostApplicationLifetime applicationLifetime,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            ExtractionSummary summary = await orchestrator.RunAsync(stoppingToken);

            foreach (ExtractionResult result in summary.Results)
            {
                logger.LogInformation(
                    "Result {SourceName}: Success={Success}, Records={RecordCount}, Duration={ElapsedMs} ms.",
                    result.SourceName,
                    result.IsSuccessful,
                    result.Records.Count,
                    result.Duration.TotalMilliseconds);
            }

            if (!summary.IsSuccessful)
            {
                Environment.ExitCode = 1;
                logger.LogWarning(
                    "The extraction finished with one or more failed sources.");
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("ETL extraction was cancelled.");
        }
        catch (Exception exception)
        {
            Environment.ExitCode = 1;
            logger.LogCritical(exception, "An unrecoverable ETL error occurred.");
        }
        finally
        {
            applicationLifetime.StopApplication();
        }
    }
}
