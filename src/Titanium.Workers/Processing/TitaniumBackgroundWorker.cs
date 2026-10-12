using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Titanium.Application.Interfaces;
using Titanium.Domain.Entities;
using Titanium.Domain.Enums;

namespace Titanium.Workers.Processing;

public class TitaniumBackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBackgroundJobQueue _jobQueue;
    private readonly ILogger<TitaniumBackgroundWorker> _logger;

    public TitaniumBackgroundWorker(
        IServiceProvider serviceProvider,
        IBackgroundJobQueue jobQueue,
        ILogger<TitaniumBackgroundWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _jobQueue = jobQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Titanium Background Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _jobQueue.DequeueNextJobAsync(stoppingToken);
                if (job != null)
                {
                    _logger.LogInformation("Processing Job {JobId} of type {JobType}", job.Id, job.JobType);

                    await ProcessJobAsync(job, stoppingToken);

                    await _jobQueue.UpdateJobStatusAsync(job.Id, JobStatus.Completed, cancellationToken: stoppingToken);
                    _logger.LogInformation("Completed Job {JobId}", job.Id);
                }
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Graceful worker shutdown initiated.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in background worker processing loop.");
                await Task.Delay(2000, stoppingToken);
            }
        }

        _logger.LogInformation("Titanium Background Worker gracefully stopped.");
    }

    private async Task ProcessJobAsync(BackgroundJob job, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ITitaniumDbContext>();
        var extractor = scope.ServiceProvider.GetService<IStatementExtractor>();
        var normalizer = scope.ServiceProvider.GetService<IClaimNormalizer>();
        var retriever = scope.ServiceProvider.GetService<IRelevantClaimRetriever>();
        var contradictionAnalyzer = scope.ServiceProvider.GetService<IClaimRelationshipAnalyzer>();

        if (job.JobType == "StatementExtraction" || job.JobType == "EmailAnalysis")
        {
            var payload = JsonSerializer.Deserialize<StatementExtractionPayload>(job.Payload);
            if (payload != null && extractor != null && normalizer != null && contradictionAnalyzer != null && retriever != null)
            {
                var communication = db.Communications.FirstOrDefault(c => c.Id == payload.CommunicationId);
                if (communication != null)
                {
                    // 1. Extract statements
                    var statements = await extractor.ExtractAsync(communication, cancellationToken);
                    foreach (var stmt in statements)
                    {
                        db.AddEntity(stmt);
                    }
                    await db.SaveChangesAsync(cancellationToken);

                    // 2. Normalize to Claims
                    var claims = await normalizer.NormalizeAsync(statements, cancellationToken);
                    foreach (var claim in claims)
                    {
                        db.AddEntity(claim);
                    }
                    await db.SaveChangesAsync(cancellationToken);

                    // 3. Correlate Claims & Detect Contradictions / Relationships
                    foreach (var claim in claims)
                    {
                        var candidates = await retriever.RetrieveRelevantCandidatesAsync(claim, payload.WorkspaceId, 20, cancellationToken);
                        if (candidates.Count > 0)
                        {
                            var relationships = await contradictionAnalyzer.AnalyzeAsync(claim, candidates, cancellationToken);
                            foreach (var rel in relationships)
                            {
                                db.AddEntity(rel);
                            }
                        }
                    }

                    await db.SaveChangesAsync(cancellationToken);
                }
            }
        }
    }

    private class StatementExtractionPayload
    {
        public Guid CommunicationId { get; set; }
        public Guid WorkspaceId { get; set; }
    }
}
