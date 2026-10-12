using Titanium.Domain.Entities;
using Titanium.Domain.Enums;

namespace Titanium.Application.Interfaces;

public interface IStatementExtractor
{
    Task<IReadOnlyList<Statement>> ExtractAsync(
        Communication communication,
        CancellationToken cancellationToken = default);
}

public interface IClaimNormalizer
{
    Task<IReadOnlyList<Claim>> NormalizeAsync(
        IReadOnlyList<Statement> statements,
        CancellationToken cancellationToken = default);
}

public interface IClaimRelationshipAnalyzer
{
    Task<IReadOnlyList<ClaimRelationship>> AnalyzeAsync(
        Claim claim,
        IReadOnlyList<Claim> candidates,
        CancellationToken cancellationToken = default);
}

public interface IRelevantClaimRetriever
{
    Task<IReadOnlyList<Claim>> RetrieveRelevantCandidatesAsync(
        Claim claim,
        Guid workspaceId,
        int maxCandidates = 20,
        CancellationToken cancellationToken = default);
}

public interface ICommunicationNormalizer
{
    string NormalizeBody(string? bodyHtml, string? bodyText);
    string StripQuotedEmailHistory(string bodyText);
    string StripSignatures(string bodyText);
    string HtmlToPlainText(string html);
    string ComputeContentHash(string content);
}

public interface IM365AuthenticationService
{
    string ClientId { get; }
    string TenantId { get; }
    void Configure(string clientId, string tenantId = "common");
    Task<bool> SignInAsync(CancellationToken cancellationToken = default);
    Task SignOutAsync(CancellationToken cancellationToken = default);
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    Task<string?> GetUserEmailAsync();
}

public class AppSettings
{
    public string M365ClientId { get; set; } = string.Empty;
    public string M365TenantId { get; set; } = "common";
    public string M365RedirectUri { get; set; } = "http://localhost";
    public string DatabasePath { get; set; } = string.Empty;
    public string DatabaseProvider { get; set; } = "SQLite";
    public string AiProvider { get; set; } = "Local / Rule-based";
    public string OpenAiApiKey { get; set; } = string.Empty;
}

public interface IAppSettingsService
{
    AppSettings GetSettings();
    Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken = default);
    Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default);
}

public interface IM365MailboxSyncService
{
    Task SyncMailboxAsync(
        Guid workspaceId,
        Guid? matterId,
        IProgress<Contracts.DTOs.SyncProgressDto>? progress = null,
        CancellationToken cancellationToken = default);
}

public interface IBackgroundJobQueue
{
    Task EnqueueJobAsync(string jobType, object payload, CancellationToken cancellationToken = default);
    Task<BackgroundJob?> DequeueNextJobAsync(CancellationToken cancellationToken = default);
    Task UpdateJobStatusAsync(Guid jobId, JobStatus status, string? error = null, CancellationToken cancellationToken = default);
}

public interface ITitaniumDbContext
{
    IQueryable<Workspace> Workspaces { get; }
    IQueryable<Matter> Matters { get; }
    IQueryable<Person> Persons { get; }
    IQueryable<Communication> Communications { get; }
    IQueryable<Conversation> Conversations { get; }
    IQueryable<Statement> Statements { get; }
    IQueryable<Claim> Claims { get; }
    IQueryable<ClaimRelationship> ClaimRelationships { get; }
    IQueryable<Event> Events { get; }
    IQueryable<EvidenceLink> EvidenceLinks { get; }
    IQueryable<Narrative> Narratives { get; }
    IQueryable<NarrativeItem> NarrativeItems { get; }
    IQueryable<IssueDossier> Dossiers { get; }
    IQueryable<AnalysisRun> AnalysisRuns { get; }
    IQueryable<BackgroundJob> BackgroundJobs { get; }

    void AddEntity<T>(T entity) where T : class;
    void UpdateEntity<T>(T entity) where T : class;
    void RemoveEntity<T>(T entity) where T : class;

    Task PurgeAllDataAsync(CancellationToken cancellationToken = default);
    Task InitializeDatabaseAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IApplicationUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
