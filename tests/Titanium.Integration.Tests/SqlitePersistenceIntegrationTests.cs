using Microsoft.EntityFrameworkCore;
using Titanium.Domain.Entities;
using Titanium.Domain.Enums;
using Titanium.Infrastructure.Persistence;
using Xunit;

namespace Titanium.Integration.Tests;

public class SqlitePersistenceIntegrationTests : IDisposable
{
    private readonly string _dbPath;
    private readonly TitaniumDbContext _context;

    public SqlitePersistenceIntegrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"titanium_test_{Guid.NewGuid()}.db");
        var options = new DbContextOptionsBuilder<TitaniumDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _context = new TitaniumDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task CanPersistAndRetrieveWorkspaceAndMatter()
    {
        var workspace = new Workspace
        {
            Name = "Legal Investigation Workspace",
            Description = "Test Workspace"
        };
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        var matter = new Matter
        {
            WorkspaceId = workspace.Id,
            Name = "Family Dispute Case #1",
            Status = MatterStatus.Active
        };
        _context.Matters.Add(matter);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Matters.FirstOrDefaultAsync(m => m.Id == matter.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Family Dispute Case #1", retrieved.Name);
    }

    [Fact]
    public async Task BackgroundJobQueue_ShouldEnqueueAndDequeueJobs()
    {
        var queue = new BackgroundJobQueue(_context);
        await queue.EnqueueJobAsync("SyncMailbox", new { Folder = "Inbox" });

        var job = await queue.DequeueNextJobAsync();
        Assert.NotNull(job);
        Assert.Equal(JobStatus.Processing, job.Status);
        Assert.Equal("SyncMailbox", job.JobType);

        await queue.UpdateJobStatusAsync(job.Id, JobStatus.Completed);
        var completedJob = await _context.BackgroundJobs.FindAsync(job.Id);
        Assert.NotNull(completedJob);
        Assert.Equal(JobStatus.Completed, completedJob.Status);
    }

    [Fact]
    public async Task PurgeAllDataAsync_ClearsAllEntitiesInDatabase()
    {
        var workspace = new Workspace
        {
            Name = "Purge Test Workspace",
            Description = "To be deleted"
        };
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        var comm = new Communication
        {
            WorkspaceId = workspace.Id,
            Subject = "Test Email to Purge",
            Sender = "sender@example.com",
            BodyText = "I will pay the bill."
        };
        _context.Communications.Add(comm);
        await _context.SaveChangesAsync();

        var claim = new Claim
        {
            WorkspaceId = workspace.Id,
            Subject = "sender@example.com",
            Predicate = "Commitment",
            Object = "pay the bill",
            NormalizedAssertion = "I will pay the bill",
            ReviewStatus = ReviewStatus.PendingReview
        };
        _context.Claims.Add(claim);
        await _context.SaveChangesAsync();

        // Verify entities exist before purge
        Assert.Equal(1, await _context.Workspaces.CountAsync());
        Assert.Equal(1, await _context.Communications.CountAsync());
        Assert.Equal(1, await _context.Claims.CountAsync());

        // Perform Purge
        await _context.PurgeAllDataAsync();

        // Verify all tables are completely empty
        Assert.Equal(0, await _context.Workspaces.CountAsync());
        Assert.Equal(0, await _context.Communications.CountAsync());
        Assert.Equal(0, await _context.Claims.CountAsync());
        Assert.Equal(0, await _context.Matters.CountAsync());
        Assert.Equal(0, await _context.ClaimRelationships.CountAsync());
        Assert.Equal(0, await _context.BackgroundJobs.CountAsync());
    }

    [Fact]
    public async Task InitializeDatabaseAsync_MigratesExistingTablesWithMissingColumns()
    {
        var legacyDbPath = Path.Combine(Path.GetTempPath(), $"titanium_legacy_{Guid.NewGuid()}.db");
        var connectionString = $"Data Source={legacyDbPath}";

        try
        {
            // 1. Manually create legacy schema missing TargetFolders column
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE Workspaces (
                        Id TEXT PRIMARY KEY,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL,
                        Name TEXT NOT NULL,
                        Description TEXT,
                        OwnerId TEXT
                    );
                    INSERT INTO Workspaces (Id, CreatedAt, UpdatedAt, Name, Description)
                    VALUES ('" + Guid.NewGuid() + @"', '2026-01-01', '2026-01-01', 'Legacy Workspace', 'Created before TargetFolders');
                ";
                await cmd.ExecuteNonQueryAsync();
            }

            // 2. Open TitaniumDbContext and run InitializeDatabaseAsync
            var options = new DbContextOptionsBuilder<TitaniumDbContext>()
                .UseSqlite(connectionString)
                .Options;

            using (var context = new TitaniumDbContext(options))
            {
                await context.InitializeDatabaseAsync();

                // 3. Query workspaces without SQLite column errors
                var workspaces = await context.Workspaces.ToListAsync();
                Assert.Single(workspaces);
                Assert.Equal("Legacy Workspace", workspaces[0].Name);
                Assert.NotNull(workspaces[0].TargetFolders);
            }
        }
        finally
        {
            if (File.Exists(legacyDbPath))
            {
                try { File.Delete(legacyDbPath); } catch { }
            }
        }
    }

    public void Dispose()
    {
        _context.Dispose();
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }
    }
}
