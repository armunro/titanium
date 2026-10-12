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
    public async Task DeleteWorkspaceAsync_DeletesWorkspaceAndCascadeCleansAssociatedData()
    {
        // 1. Create two workspaces
        var ws1 = new Workspace { Name = "Workspace to Delete", Description = "WS1" };
        var ws2 = new Workspace { Name = "Workspace to Keep", Description = "WS2" };
        _context.Workspaces.AddRange(ws1, ws2);
        await _context.SaveChangesAsync();

        // 2. Add matters, communications, statements, claims, and relationships to ws1
        var matter1 = new Matter { WorkspaceId = ws1.Id, Name = "Matter in WS1" };
        var matter2 = new Matter { WorkspaceId = ws2.Id, Name = "Matter in WS2" };
        _context.Matters.AddRange(matter1, matter2);
        await _context.SaveChangesAsync();

        var comm1 = new Communication { WorkspaceId = ws1.Id, Subject = "WS1 Email", Sender = "a@test.com", BodyText = "Text 1" };
        var comm2 = new Communication { WorkspaceId = ws2.Id, Subject = "WS2 Email", Sender = "b@test.com", BodyText = "Text 2" };
        _context.Communications.AddRange(comm1, comm2);
        await _context.SaveChangesAsync();

        var stmt1 = new Statement { CommunicationId = comm1.Id, OriginalText = "Text 1 statement", SpeakerName = "a@test.com" };
        _context.Statements.Add(stmt1);
        await _context.SaveChangesAsync();

        var claim1 = new Claim { WorkspaceId = ws1.Id, Subject = "a@test.com", Predicate = "Fact", Object = "assertion 1" };
        var claim2 = new Claim { WorkspaceId = ws1.Id, Subject = "a@test.com", Predicate = "Fact", Object = "assertion 2" };
        var claim3 = new Claim { WorkspaceId = ws2.Id, Subject = "b@test.com", Predicate = "Fact", Object = "assertion 3" };
        _context.Claims.AddRange(claim1, claim2, claim3);
        await _context.SaveChangesAsync();

        var rel1 = new ClaimRelationship { SourceClaimId = claim1.Id, TargetClaimId = claim2.Id, RelationshipType = ClaimRelationshipType.Contradicts, Explanation = "Contradiction test" };
        _context.ClaimRelationships.Add(rel1);
        await _context.SaveChangesAsync();

        // Verify entities exist
        Assert.Equal(2, await _context.Workspaces.CountAsync());
        Assert.Equal(2, await _context.Matters.CountAsync());
        Assert.Equal(2, await _context.Communications.CountAsync());
        Assert.Equal(3, await _context.Claims.CountAsync());
        Assert.Equal(1, await _context.ClaimRelationships.CountAsync());

        // 3. Delete ws1
        await _context.DeleteWorkspaceAsync(ws1.Id);

        // 4. Verify ws1 and its associated data are removed, while ws2 data remains
        var remainingWorkspaces = await _context.Workspaces.ToListAsync();
        Assert.Single(remainingWorkspaces);
        Assert.Equal(ws2.Id, remainingWorkspaces[0].Id);

        var remainingMatters = await _context.Matters.ToListAsync();
        Assert.Single(remainingMatters);
        Assert.Equal(matter2.Id, remainingMatters[0].Id);

        var remainingComms = await _context.Communications.ToListAsync();
        Assert.Single(remainingComms);
        Assert.Equal(comm2.Id, remainingComms[0].Id);

        Assert.Empty(await _context.Statements.Where(s => s.CommunicationId == comm1.Id).ToListAsync());
        Assert.Empty(await _context.ClaimRelationships.Where(r => r.SourceClaimId == claim1.Id || r.TargetClaimId == claim2.Id).ToListAsync());

        var remainingClaims = await _context.Claims.ToListAsync();
        Assert.Single(remainingClaims);
        Assert.Equal(claim3.Id, remainingClaims[0].Id);
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
