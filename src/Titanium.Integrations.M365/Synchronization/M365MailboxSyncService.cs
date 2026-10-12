using System.Net.Http.Headers;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using Titanium.Application.Interfaces;
using Titanium.Contracts.DTOs;
using Titanium.Domain.Entities;
using Titanium.Domain.Enums;
using Titanium.Domain.Rules;

namespace Titanium.Integrations.M365.Synchronization;

public class M365MailboxSyncService : IM365MailboxSyncService
{
    private readonly ITitaniumDbContext _context;
    private readonly IM365AuthenticationService _authService;
    private readonly ICommunicationNormalizer _normalizer;
    private readonly IBackgroundJobQueue _jobQueue;

    public M365MailboxSyncService(
        ITitaniumDbContext context,
        IM365AuthenticationService authService,
        ICommunicationNormalizer normalizer,
        IBackgroundJobQueue jobQueue)
    {
        _context = context;
        _authService = authService;
        _normalizer = normalizer;
        _jobQueue = jobQueue;
    }

    public async Task SyncMailboxAsync(
        Guid workspaceId,
        Guid? matterId,
        IProgress<SyncProgressDto>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var workspace = _context.Workspaces.FirstOrDefault(w => w.Id == workspaceId);
        var targetFolders = workspace?.TargetFolders?.Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f.Trim()).Distinct().ToList() ?? new List<string>();
        if (targetFolders.Count == 0)
        {
            targetFolders = new List<string> { "Inbox" };
        }

        if (string.IsNullOrWhiteSpace(_authService.ClientId))
        {
            progress?.Report(new SyncProgressDto(0, 0, string.Join(", ", targetFolders), "Configuration Required", true, "Microsoft 365 Client ID is not configured. Please enter your Client ID and Tenant ID in the Settings tab."));
            return;
        }

        progress?.Report(new SyncProgressDto(0, 0, string.Join(", ", targetFolders), "Checking authentication...", false));

        var isAuthenticated = await _authService.IsAuthenticatedAsync();
        if (!isAuthenticated)
        {
            var signedIn = await _authService.SignInAsync(cancellationToken);
            if (!signedIn)
            {
                progress?.Report(new SyncProgressDto(0, 0, string.Join(", ", targetFolders), "Authentication failed", true, "Could not authenticate with Microsoft 365. Please verify your Client ID and Tenant ID in Settings."));
                return;
            }
        }

        var token = await _authService.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            progress?.Report(new SyncProgressDto(0, 0, string.Join(", ", targetFolders), "Access token acquisition failed", true, "Access token is empty"));
            return;
        }

        progress?.Report(new SyncProgressDto(100, 10, string.Join(", ", targetFolders), "Connecting to Microsoft Graph...", false));

        try
        {
            var authProvider = new BaseBearerTokenAuthenticationProvider(new TokenProvider(token));
            var graphClient = new GraphServiceClient(authProvider);

            var totalImportedCount = 0;
            var folderIndex = 0;

            foreach (var folder in targetFolders)
            {
                if (cancellationToken.IsCancellationRequested) break;
                folderIndex++;

                progress?.Report(new SyncProgressDto(100, 20, folder, $"Retrieving messages from folder '{folder}' ({folderIndex}/{targetFolders.Count})...", false));

                var messages = await GetMessagesFromFolderAsync(graphClient, folder, cancellationToken);
                var totalMessages = messages.Count;
                var processed = 0;

                progress?.Report(new SyncProgressDto(totalMessages, processed, folder, $"Found {totalMessages} messages in '{folder}'. Processing...", false));

                foreach (var message in messages)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    processed++;
                    var externalId = message.Id ?? Guid.NewGuid().ToString();
                    var internetMessageId = message.InternetMessageId ?? string.Empty;
                    var sender = message.From?.EmailAddress?.Address ?? "unknown@sender.com";
                    var recipients = (message.ToRecipients ?? new List<Recipient>())
                        .Select(r => r.EmailAddress?.Address)
                        .Where(a => !string.IsNullOrWhiteSpace(a))
                        .Select(a => a!)
                        .ToList();

                    var subject = message.Subject ?? "(No Subject)";
                    var isHtml = message.Body?.ContentType == BodyType.Html;
                    var bodyHtml = isHtml ? (message.Body?.Content ?? string.Empty) : string.Empty;
                    var bodyText = !isHtml ? (message.Body?.Content ?? string.Empty) : string.Empty;
                    var sentAt = message.SentDateTime?.UtcDateTime ?? message.ReceivedDateTime?.UtcDateTime ?? DateTime.UtcNow;

                    var communication = await ImportEmailAsync(
                        workspaceId,
                        matterId,
                        externalId,
                        internetMessageId,
                        sender,
                        recipients,
                        subject,
                        bodyHtml,
                        bodyText,
                        sentAt,
                        cancellationToken);

                    if (communication != null)
                    {
                        totalImportedCount++;
                        // Queue for AI analysis
                        await _jobQueue.EnqueueJobAsync(
                            "StatementExtraction",
                            new { CommunicationId = communication.Id, WorkspaceId = workspaceId },
                            cancellationToken);
                    }

                    progress?.Report(new SyncProgressDto(totalMessages, processed, folder, $"[{folder}] Imported: {subject}", false));
                }
            }

            progress?.Report(new SyncProgressDto(100, 100, string.Join(", ", targetFolders), $"Mailbox synchronization completed across {targetFolders.Count} folder(s). {totalImportedCount} communications processed.", true));
        }
        catch (Exception ex)
        {
            progress?.Report(new SyncProgressDto(0, 0, string.Join(", ", targetFolders), "Sync Error", true, $"Error synchronizing messages: {ex.Message}"));
        }
    }

    private static async Task<List<Message>> GetMessagesFromFolderAsync(GraphServiceClient graphClient, string folderName, CancellationToken cancellationToken)
    {
        try
        {
            var folderId = await ResolveFolderIdAsync(graphClient, folderName, cancellationToken);
            if (!string.IsNullOrEmpty(folderId))
            {
                var folderMessagesResponse = await graphClient.Me.MailFolders[folderId].Messages.GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Top = 50;
                    requestConfiguration.QueryParameters.Select = new[]
                    {
                        "id", "internetMessageId", "conversationId", "subject", "from", "toRecipients", "ccRecipients",
                        "sentDateTime", "receivedDateTime", "body", "uniqueBody", "hasAttachments"
                    };
                    requestConfiguration.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
                }, cancellationToken);

                return folderMessagesResponse?.Value ?? new List<Message>();
            }
        }
        catch
        {
            // Ignore search error and return empty list
        }

        return new List<Message>();
    }

    public static async Task<string?> ResolveFolderIdAsync(GraphServiceClient graphClient, string folderPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath)) return null;

        // Clean up escaped backslashes and normalize separators
        var normalizedPath = folderPath.Trim().Replace("\\\\", "\\").Replace('/', '\\');

        // 1. If it's a simple well-known folder name (Inbox, Drafts, SentItems, Archive, etc.) or direct ID
        var segments = normalizedPath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0) return null;

        if (segments.Length == 1)
        {
            var singleName = segments[0];
            try
            {
                // Try direct well-known access
                var directFolder = await graphClient.Me.MailFolders[singleName].GetAsync(cancellationToken: cancellationToken);
                if (directFolder?.Id != null) return directFolder.Id;
            }
            catch
            {
                // Ignore and try searching top-level folders
            }
        }

        // 2. Hierarchical navigation through child folders
        try
        {
            var topFoldersResponse = await graphClient.Me.MailFolders.GetAsync(config =>
            {
                config.QueryParameters.Top = 100;
            }, cancellationToken);

            var topFolders = topFoldersResponse?.Value ?? new List<MailFolder>();

            // Find matching root folder
            var currentFolder = topFolders.FirstOrDefault(f =>
                string.Equals(f.DisplayName, segments[0], StringComparison.OrdinalIgnoreCase) ||
                string.Equals(f.Id, segments[0], StringComparison.OrdinalIgnoreCase));

            if (currentFolder == null)
            {
                // If single segment didn't match top level, check if any subfolder across tree matches
                return await FindFolderByNameRecursiveAsync(graphClient, topFolders, segments[segments.Length - 1], cancellationToken);
            }

            for (int i = 1; i < segments.Length; i++)
            {
                var segmentName = segments[i];
                if (currentFolder?.Id == null) return null;

                var childFoldersResponse = await graphClient.Me.MailFolders[currentFolder.Id].ChildFolders.GetAsync(config =>
                {
                    config.QueryParameters.Top = 100;
                }, cancellationToken);

                var childFolders = childFoldersResponse?.Value ?? new List<MailFolder>();
                currentFolder = childFolders.FirstOrDefault(f =>
                    string.Equals(f.DisplayName, segmentName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f.Id, segmentName, StringComparison.OrdinalIgnoreCase));

                if (currentFolder == null) return null;
            }

            return currentFolder?.Id;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> FindFolderByNameRecursiveAsync(GraphServiceClient graphClient, List<MailFolder> folders, string targetName, CancellationToken cancellationToken)
    {
        foreach (var folder in folders)
        {
            if (string.Equals(folder.DisplayName, targetName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(folder.Id, targetName, StringComparison.OrdinalIgnoreCase))
            {
                return folder.Id;
            }

            if (folder.Id != null && folder.ChildFolderCount.GetValueOrDefault() > 0)
            {
                try
                {
                    var childResponse = await graphClient.Me.MailFolders[folder.Id].ChildFolders.GetAsync(config =>
                    {
                        config.QueryParameters.Top = 100;
                    }, cancellationToken);

                    var children = childResponse?.Value ?? new List<MailFolder>();
                    var foundChild = await FindFolderByNameRecursiveAsync(graphClient, children, targetName, cancellationToken);
                    if (foundChild != null) return foundChild;
                }
                catch
                {
                    // Ignore errors navigating child folders
                }
            }
        }

        return null;
    }

    public async Task<Communication> ImportEmailAsync(
        Guid workspaceId,
        Guid? matterId,
        string externalId,
        string internetMessageId,
        string sender,
        List<string> recipients,
        string subject,
        string bodyHtml,
        string bodyText,
        DateTime sentAt,
        CancellationToken cancellationToken = default)
    {
        var normalizedBody = _normalizer.NormalizeBody(bodyHtml, bodyText);
        var hash = _normalizer.ComputeContentHash(normalizedBody);

        // Duplicate check by hash or message id
        var existing = _context.Communications
            .FirstOrDefault(c => c.WorkspaceId == workspaceId && (c.ExternalMessageId == externalId || c.OriginalContentHash == hash));

        if (existing != null)
        {
            return existing;
        }

        // Thread resolution / Conversation linking
        var cleanSubject = DomainValidationRules.NormalizeSubject(subject);
        var conversation = _context.Conversations
            .FirstOrDefault(c => c.WorkspaceId == workspaceId && c.Subject == cleanSubject);

        if (conversation == null)
        {
            conversation = new Domain.Entities.Conversation
            {
                WorkspaceId = workspaceId,
                Subject = cleanSubject,
                StartedAt = sentAt,
                LastActivityAt = sentAt,
                Participants = new List<string>(recipients) { sender }.Distinct().ToList()
            };
            _context.AddEntity(conversation);
        }
        else
        {
            if (sentAt > (conversation.LastActivityAt ?? DateTime.MinValue))
            {
                conversation.LastActivityAt = sentAt;
            }
            foreach (var r in recipients.Concat(new[] { sender }))
            {
                if (!conversation.Participants.Contains(r)) conversation.Participants.Add(r);
            }
            _context.UpdateEntity(conversation);
        }

        var communication = new Communication
        {
            WorkspaceId = workspaceId,
            MatterId = matterId,
            ConversationId = conversation.Id,
            Source = CommunicationSource.Microsoft365,
            ExternalMessageId = externalId,
            InternetMessageId = internetMessageId,
            Sender = sender,
            Recipients = recipients,
            Subject = subject,
            BodyText = normalizedBody,
            BodyHtml = bodyHtml,
            SentAt = sentAt,
            ReceivedAt = sentAt,
            ImportedAt = DateTime.UtcNow,
            OriginalContentHash = hash
        };

        _context.AddEntity(communication);
        await _context.SaveChangesAsync(cancellationToken);

        return communication;
    }

    private class TokenProvider : IAccessTokenProvider
    {
        private readonly string _token;

        public TokenProvider(string token)
        {
            _token = token;
        }

        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_token);
        }

        public AllowedHostsValidator AllowedHostsValidator { get; } = new();
    }
}
