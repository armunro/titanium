using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Titanium.Application.Interfaces;
using Titanium.Domain.Entities;
using Titanium.Domain.Enums;

namespace Titanium.Infrastructure.Persistence;

public class BackgroundJobQueue : IBackgroundJobQueue
{
    private readonly TitaniumDbContext _context;

    public BackgroundJobQueue(TitaniumDbContext context)
    {
        _context = context;
    }

    public async Task EnqueueJobAsync(string jobType, object payload, CancellationToken cancellationToken = default)
    {
        var job = new BackgroundJob
        {
            JobType = jobType,
            Payload = JsonSerializer.Serialize(payload),
            Status = JobStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.BackgroundJobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<BackgroundJob?> DequeueNextJobAsync(CancellationToken cancellationToken = default)
    {
        var job = await _context.BackgroundJobs
            .Where(j => j.Status == JobStatus.Pending || (j.Status == JobStatus.Retry && j.AttemptCount < j.MaxAttempts))
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (job != null)
        {
            job.Status = JobStatus.Processing;
            job.StartedAt = DateTime.UtcNow;
            job.AttemptCount++;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return job;
    }

    public async Task UpdateJobStatusAsync(Guid jobId, JobStatus status, string? error = null, CancellationToken cancellationToken = default)
    {
        var job = await _context.BackgroundJobs.FindAsync(new object[] { jobId }, cancellationToken);
        if (job != null)
        {
            job.Status = status;
            job.LastError = error;
            if (status == JobStatus.Completed || status == JobStatus.Failed || status == JobStatus.Cancelled)
            {
                job.CompletedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
