using Titanium.Application.Interfaces;
using Titanium.Application.Services;
using Titanium.Contracts.DTOs;
using Titanium.Domain.Entities;
using Titanium.Domain.Enums;
using Titanium.Infrastructure.Persistence;
using Titanium.Integrations.M365.Synchronization;
using Titanium.Intelligence.Extraction;
using Titanium.Intelligence.Classification;
using Titanium.Intelligence.Contradictions;
using Titanium.Intelligence.Retrieval;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Titanium.Integration.Tests;

public class EmailSyncAndIntelligencePipelineTests
{
    private TitaniumDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TitaniumDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        var context = new TitaniumDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task ImportEmailAsync_CreatesNormalizedCommunicationAndConversation()
    {
        using var context = CreateInMemoryContext();
        var normalizer = new CommunicationNormalizer();
        var jobQueue = new BackgroundJobQueue(context);
        var mockAuth = new MockAuthService();
        var syncService = new M365MailboxSyncService(context, mockAuth, normalizer, jobQueue);

        var workspace = new Workspace { Name = "Legal Dispute Workspace" };
        context.AddEntity(workspace);
        await context.SaveChangesAsync();

        var email = await syncService.ImportEmailAsync(
            workspace.Id,
            null,
            "ext-msg-123",
            "<msg123@office365.com>",
            "alice@example.com",
            new List<string> { "bob@example.com" },
            "RE: Parenting Schedule for June",
            "<p>I agreed to take the kids on Friday afternoon.</p>",
            "I agreed to take the kids on Friday afternoon.",
            DateTime.UtcNow);

        Assert.NotNull(email);
        Assert.Equal("RE: Parenting Schedule for June", email.Subject);
        Assert.Equal("alice@example.com", email.Sender);
        Assert.Equal(CommunicationSource.Microsoft365, email.Source);

        var conversations = await context.Conversations.ToListAsync();
        Assert.Single(conversations);
        Assert.Equal("Parenting Schedule for June", conversations[0].Subject);
        Assert.Contains("alice@example.com", conversations[0].Participants);
        Assert.Contains("bob@example.com", conversations[0].Participants);
    }

    [Fact]
    public async Task ImportEmailAsync_IgnoresDuplicateEmailBasedOnContentHash()
    {
        using var context = CreateInMemoryContext();
        var normalizer = new CommunicationNormalizer();
        var jobQueue = new BackgroundJobQueue(context);
        var mockAuth = new MockAuthService();
        var syncService = new M365MailboxSyncService(context, mockAuth, normalizer, jobQueue);

        var workspace = new Workspace { Name = "Legal Dispute Workspace" };
        context.AddEntity(workspace);
        await context.SaveChangesAsync();

        var email1 = await syncService.ImportEmailAsync(
            workspace.Id,
            null,
            "ext-1",
            "<msg1@office365.com>",
            "alice@example.com",
            new List<string> { "bob@example.com" },
            "Schedule update",
            "<p>Original message text.</p>",
            "Original message text.",
            DateTime.UtcNow);

        var email2 = await syncService.ImportEmailAsync(
            workspace.Id,
            null,
            "ext-2",
            "<msg2@office365.com>",
            "alice@example.com",
            new List<string> { "bob@example.com" },
            "Schedule update",
            "<p>Original message text.</p>",
            "Original message text.",
            DateTime.UtcNow);

        Assert.Equal(email1.Id, email2.Id);
        var totalComms = await context.Communications.CountAsync();
        Assert.Equal(1, totalComms);
    }

    [Fact]
    public async Task SqliteForeignKeys_EnforcedAndSatisfiedWhenWorkspacePersisted()
    {
        using var context = CreateInMemoryContext();
        
        var workspace = new Workspace { Name = "Enforced FK Workspace" };
        context.AddEntity(workspace);
        await context.SaveChangesAsync();

        var matter = new Matter { Name = "Dispute Case 1", WorkspaceId = workspace.Id };
        context.AddEntity(matter);
        await context.SaveChangesAsync();

        var comm = new Communication
        {
            WorkspaceId = workspace.Id,
            MatterId = matter.Id,
            Sender = "sender@domain.com",
            Subject = "Test Subject",
            OriginalContentHash = "hash123"
        };
        context.AddEntity(comm);
        await context.SaveChangesAsync();

        var retrievedComm = await context.Communications.FirstOrDefaultAsync(c => c.Id == comm.Id);
        Assert.NotNull(retrievedComm);
        Assert.Equal(workspace.Id, retrievedComm.WorkspaceId);
        Assert.Equal(matter.Id, retrievedComm.MatterId);
    }

    [Fact]
    public async Task PurgeAllDataAsync_ClearsSyncAndIntelligenceData()
    {
        using var context = CreateInMemoryContext();
        var normalizer = new CommunicationNormalizer();
        var jobQueue = new BackgroundJobQueue(context);
        var mockAuth = new MockAuthService();
        var syncService = new M365MailboxSyncService(context, mockAuth, normalizer, jobQueue);

        var workspace = new Workspace { Name = "Purge Case Workspace" };
        context.AddEntity(workspace);
        await context.SaveChangesAsync();

        var comm = await syncService.ImportEmailAsync(
            workspace.Id,
            null,
            "ext-purge-1",
            "<purge1@test.com>",
            "alice@example.com",
            new List<string> { "bob@example.com" },
            "Purge Subject",
            "<p>Statement to purge.</p>",
            "Statement to purge.",
            DateTime.UtcNow);

        Assert.NotNull(comm);
        await jobQueue.EnqueueJobAsync("StatementExtraction", new { CommunicationId = comm.Id });

        Assert.Equal(1, await context.Communications.CountAsync());
        Assert.Equal(1, await context.Conversations.CountAsync());
        Assert.Equal(1, await context.BackgroundJobs.CountAsync());

        // Perform Purge
        await context.PurgeAllDataAsync();

        // Verify all entities were removed
        Assert.Equal(0, await context.Communications.CountAsync());
        Assert.Equal(0, await context.Conversations.CountAsync());
        Assert.Equal(0, await context.BackgroundJobs.CountAsync());
        Assert.Equal(0, await context.Workspaces.CountAsync());
    }

    [Fact]
    public async Task StatementsAndClaims_LinkedToCommunication_AreRetrievablePerCommunication()
    {
        using var context = CreateInMemoryContext();
        var workspace = new Workspace { Name = "Per Email Workspace" };
        context.AddEntity(workspace);
        await context.SaveChangesAsync();

        var comm1 = new Communication
        {
            WorkspaceId = workspace.Id,
            Subject = "Email 1",
            Sender = "alice@example.com",
            BodyText = "I will attend the meeting on Friday."
        };
        var comm2 = new Communication
        {
            WorkspaceId = workspace.Id,
            Subject = "Email 2",
            Sender = "bob@example.com",
            BodyText = "I cannot make it."
        };
        context.AddEntity(comm1);
        context.AddEntity(comm2);
        await context.SaveChangesAsync();

        var claim1 = new Claim
        {
            WorkspaceId = workspace.Id,
            Subject = "alice@example.com",
            Predicate = "Commitment",
            Object = "attend meeting on Friday",
            NormalizedAssertion = "Alice will attend meeting on Friday"
        };
        var claim2 = new Claim
        {
            WorkspaceId = workspace.Id,
            Subject = "bob@example.com",
            Predicate = "Denial",
            Object = "cannot make it",
            NormalizedAssertion = "Bob cannot make it"
        };
        context.AddEntity(claim1);
        context.AddEntity(claim2);
        await context.SaveChangesAsync();

        var stmt1 = new Statement
        {
            CommunicationId = comm1.Id,
            ClaimId = claim1.Id,
            OriginalText = "I will attend the meeting on Friday."
        };
        var stmt2 = new Statement
        {
            CommunicationId = comm2.Id,
            ClaimId = claim2.Id,
            OriginalText = "I cannot make it."
        };
        context.AddEntity(stmt1);
        context.AddEntity(stmt2);

        var rel = new ClaimRelationship
        {
            SourceClaimId = claim1.Id,
            TargetClaimId = claim2.Id,
            RelationshipType = ClaimRelationshipType.Contradicts,
            Explanation = "Conflicting availability statements"
        };
        context.AddEntity(rel);
        await context.SaveChangesAsync();

        // Query for comm1 claims
        var comm1ClaimIds = await context.Statements
            .Where(s => s.CommunicationId == comm1.Id && s.ClaimId.HasValue)
            .Select(s => s.ClaimId!.Value)
            .ToListAsync();
        var comm1Claims = await context.Claims.Where(c => comm1ClaimIds.Contains(c.Id)).ToListAsync();

        Assert.Single(comm1Claims);
        Assert.Equal("Alice will attend meeting on Friday", comm1Claims[0].NormalizedAssertion);

        // Query for comm1 relationships
        var comm1Rels = await context.ClaimRelationships
            .Where(r => comm1ClaimIds.Contains(r.SourceClaimId) || comm1ClaimIds.Contains(r.TargetClaimId))
            .ToListAsync();

        Assert.Single(comm1Rels);
        Assert.Equal(ClaimRelationshipType.Contradicts, comm1Rels[0].RelationshipType);
    }

    [Fact]
    public async Task Workspace_TargetFolders_PersistAndAreConfigurable()
    {
        using var context = CreateInMemoryContext();
        var workspace = new Workspace
        {
            Name = "Dispute Workspace",
            TargetFolders = new List<string> { "Dispute", "Parenting Schedule", "Inbox" }
        };
        context.AddEntity(workspace);
        await context.SaveChangesAsync();

        var retrieved = await context.Workspaces.FirstAsync(w => w.Id == workspace.Id);
        Assert.Equal(3, retrieved.TargetFolders.Count);
        Assert.Contains("Dispute", retrieved.TargetFolders);
        Assert.Contains("Parenting Schedule", retrieved.TargetFolders);
        Assert.Contains("Inbox", retrieved.TargetFolders);

        // Update target folders
        retrieved.TargetFolders = new List<string> { "Legal Only" };
        context.UpdateEntity(retrieved);
        await context.SaveChangesAsync();

        var updated = await context.Workspaces.FirstAsync(w => w.Id == workspace.Id);
        Assert.Single(updated.TargetFolders);
        Assert.Equal("Legal Only", updated.TargetFolders[0]);
    }

    [Theory]
    [InlineData("Family\\Separation")]
    [InlineData("Family\\\\Separation")]
    [InlineData("Family/Separation")]
    [InlineData("Inbox")]
    public async Task Workspace_TargetFolders_HandlesNestedFolderPaths(string folderPath)
    {
        using var context = CreateInMemoryContext();
        var workspace = new Workspace
        {
            Name = "Family Matter",
            TargetFolders = new List<string> { folderPath }
        };
        context.AddEntity(workspace);
        await context.SaveChangesAsync();

        var retrieved = await context.Workspaces.FirstAsync(w => w.Id == workspace.Id);
        Assert.Single(retrieved.TargetFolders);
        Assert.Equal(folderPath, retrieved.TargetFolders[0]);
    }

    private class MockAuthService : IM365AuthenticationService
    {
        public string ClientId => "test-client";
        public string TenantId => "common";
        public void Configure(string clientId, string tenantId = "common") { }
        public Task<bool> SignInAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task SignOutAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> IsAuthenticatedAsync() => Task.FromResult(true);
        public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>("mock-token");
        public Task<string?> GetUserEmailAsync() => Task.FromResult<string?>("user@office365.com");
    }
}
