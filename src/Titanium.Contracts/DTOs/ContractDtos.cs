namespace Titanium.Contracts.DTOs;

public record SyncProgressDto(
    int TotalItems,
    int ProcessedItems,
    string CurrentFolder,
    string StatusMessage,
    bool IsCompleted,
    string? ErrorMessage = null);

public record AIProviderSettingsDto(
    string ProviderName, // Local, OpenAI, Claude, AzureOpenAI
    string? ApiKey,
    string? Endpoint,
    string ModelName,
    bool EnableCloudAnalysis);

public record M365AccountConfigDto(
    string ClientId,
    string TenantId,
    string? RedirectUri,
    string? UserPrincipalName,
    bool IsConnected);

public record AnalysisFilterDto(
    Guid? MatterId,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Participant,
    string? Keyword);
