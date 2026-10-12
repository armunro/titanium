using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Titanium.Application.Interfaces;
using Titanium.Contracts.DTOs;
using Titanium.Domain.Entities;

namespace Titanium.Desktop.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ITitaniumDbContext _dbContext;
    private readonly IM365MailboxSyncService _syncService;
    private readonly IM365AuthenticationService _authService;
    private readonly IAppSettingsService _settingsService;

    public ObservableCollection<Workspace> Workspaces { get; } = new();
    public ObservableCollection<Matter> Matters { get; } = new();
    public ObservableCollection<Communication> Communications { get; } = new();
    public ObservableCollection<Claim> Claims { get; } = new();
    public ObservableCollection<ClaimRelationship> Relationships { get; } = new();

    public ObservableCollection<Claim> SelectedCommunicationClaims { get; } = new();
    public ObservableCollection<ClaimRelationship> SelectedCommunicationRelationships { get; } = new();

    private Workspace? _selectedWorkspace;
    public Workspace? SelectedWorkspace
    {
        get => _selectedWorkspace;
        set
        {
            if (SetProperty(ref _selectedWorkspace, value))
            {
                UpdateWorkspaceFoldersText();
                _ = LoadMattersAndCommunicationsAsync();
            }
        }
    }

    private string _workspaceTargetFoldersText = "Inbox";
    public string WorkspaceTargetFoldersText
    {
        get => _workspaceTargetFoldersText;
        set
        {
            if (SetProperty(ref _workspaceTargetFoldersText, value))
            {
                ApplyWorkspaceTargetFoldersFromText();
            }
        }
    }

    private Matter? _selectedMatter;
    public Matter? SelectedMatter
    {
        get => _selectedMatter;
        set => SetProperty(ref _selectedMatter, value);
    }

    private Communication? _selectedCommunication;
    public Communication? SelectedCommunication
    {
        get => _selectedCommunication;
        set
        {
            if (SetProperty(ref _selectedCommunication, value))
            {
                _ = UpdateSelectedCommunicationIntelligenceAsync();
            }
        }
    }

    private int _totalCommunicationsCount;
    public int TotalCommunicationsCount
    {
        get => _totalCommunicationsCount;
        set => SetProperty(ref _totalCommunicationsCount, value);
    }

    private int _totalClaimsCount;
    public int TotalClaimsCount
    {
        get => _totalClaimsCount;
        set => SetProperty(ref _totalClaimsCount, value);
    }

    private int _contradictionsCount;
    public int ContradictionsCount
    {
        get => _contradictionsCount;
        set => SetProperty(ref _contradictionsCount, value);
    }

    // Settings Properties
    private string _m365ClientId = string.Empty;
    public string M365ClientId
    {
        get => _m365ClientId;
        set => SetProperty(ref _m365ClientId, value);
    }

    private string _m365TenantId = "common";
    public string M365TenantId
    {
        get => _m365TenantId;
        set => SetProperty(ref _m365TenantId, value);
    }

    private string _m365RedirectUri = "http://localhost";
    public string M365RedirectUri
    {
        get => _m365RedirectUri;
        set => SetProperty(ref _m365RedirectUri, value);
    }

    private string _databasePath = string.Empty;
    public string DatabasePath
    {
        get => _databasePath;
        set => SetProperty(ref _databasePath, value);
    }

    private string _databaseProvider = "SQLite (Embedded / Portable / WAL Mode)";
    public string DatabaseProvider
    {
        get => _databaseProvider;
        set => SetProperty(ref _databaseProvider, value);
    }

    private string _aiProvider = "Local / Rule-based (Deterministic)";
    public string AiProvider
    {
        get => _aiProvider;
        set => SetProperty(ref _aiProvider, value);
    }

    private string _settingsStatusMessage = string.Empty;
    public string SettingsStatusMessage
    {
        get => _settingsStatusMessage;
        set => SetProperty(ref _settingsStatusMessage, value);
    }

    private string _m365AccountStatus = "Not Connected";
    public string M365AccountStatus
    {
        get => _m365AccountStatus;
        set => SetProperty(ref _m365AccountStatus, value);
    }

    private bool _isM365Authenticated;
    public bool IsM365Authenticated
    {
        get => _isM365Authenticated;
        set => SetProperty(ref _isM365Authenticated, value);
    }

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    private string _statusMessage = "Titanium Intelligence Engine Ready | Embedded SQLite Active";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public IAsyncRelayCommand RefreshDataCommand { get; }
    public IAsyncRelayCommand SyncMailboxCommand { get; }
    public IAsyncRelayCommand CreateWorkspaceCommand { get; }
    public IAsyncRelayCommand<Workspace> DeleteWorkspaceCommand { get; }
    public IAsyncRelayCommand SaveWorkspaceFoldersCommand { get; }
    public IAsyncRelayCommand<Workspace> OpenWorkspaceSettingsCommand { get; }
    public IAsyncRelayCommand SaveSettingsCommand { get; }
    public IAsyncRelayCommand TestM365ConnectionCommand { get; }
    public IAsyncRelayCommand SignOutM365Command { get; }
    public IAsyncRelayCommand PurgeDatabaseCommand { get; }
    public IRelayCommand OpenSettingsCommand { get; }

    public MainViewModel(
        ITitaniumDbContext dbContext,
        IM365MailboxSyncService syncService,
        IM365AuthenticationService authService,
        IAppSettingsService settingsService)
    {
        _dbContext = dbContext;
        _syncService = syncService;
        _authService = authService;
        _settingsService = settingsService;
        Title = "Titanium — Communication Intelligence & Evidence Management Platform";

        RefreshDataCommand = new AsyncRelayCommand(LoadDataAsync);
        SyncMailboxCommand = new AsyncRelayCommand(ExecuteSyncMailboxAsync);
        CreateWorkspaceCommand = new AsyncRelayCommand(CreateDefaultWorkspaceAsync);
        DeleteWorkspaceCommand = new AsyncRelayCommand<Workspace>(ExecuteDeleteWorkspaceAsync);
        SaveWorkspaceFoldersCommand = new AsyncRelayCommand(ExecuteSaveWorkspaceFoldersAsync);
        OpenWorkspaceSettingsCommand = new AsyncRelayCommand<Workspace>(ExecuteOpenWorkspaceSettingsAsync);
        SaveSettingsCommand = new AsyncRelayCommand(ExecuteSaveSettingsAsync);
        TestM365ConnectionCommand = new AsyncRelayCommand(ExecuteTestM365ConnectionAsync);
        SignOutM365Command = new AsyncRelayCommand(ExecuteSignOutM365Async);
        PurgeDatabaseCommand = new AsyncRelayCommand(ExecutePurgeDatabaseAsync);
        OpenSettingsCommand = new RelayCommand(() => SelectedTabIndex = 3);
    }

    public async Task ExecuteOpenWorkspaceSettingsAsync(Workspace? workspace)
    {
        var targetWorkspace = workspace ?? SelectedWorkspace;
        if (targetWorkspace == null) return;

        SelectedWorkspace = targetWorkspace;

        if (System.Windows.Application.Current != null)
        {
            var dialog = new Views.WorkspaceSettingsDialog(targetWorkspace)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                if (dialog.IsDeleted)
                {
                    await DeleteWorkspaceInternalAsync(targetWorkspace, promptUser: false);
                }
                else
                {
                    _dbContext.UpdateEntity(targetWorkspace);
                    await _dbContext.SaveChangesAsync();

                    UpdateWorkspaceFoldersText();
                    OnPropertyChanged(nameof(Workspaces));
                    OnPropertyChanged(nameof(SelectedWorkspace));

                    StatusMessage = $"Workspace '{targetWorkspace.Name}' settings saved (Folders: {string.Join(", ", targetWorkspace.TargetFolders ?? new List<string>())}).";
                }
            }
        }
    }

    private void UpdateWorkspaceFoldersText()
    {
        if (SelectedWorkspace == null)
        {
            _workspaceTargetFoldersText = "Inbox";
            OnPropertyChanged(nameof(WorkspaceTargetFoldersText));
            return;
        }

        var folders = SelectedWorkspace.TargetFolders ?? new List<string>();
        _workspaceTargetFoldersText = folders.Count > 0 ? string.Join(", ", folders) : "Inbox";
        OnPropertyChanged(nameof(WorkspaceTargetFoldersText));
    }

    private void ApplyWorkspaceTargetFoldersFromText()
    {
        if (SelectedWorkspace == null) return;

        var list = _workspaceTargetFoldersText
            .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrEmpty(f))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (list.Count == 0)
        {
            list = new List<string> { "Inbox" };
        }

        SelectedWorkspace.TargetFolders = list;
    }

    private async Task ExecuteSaveWorkspaceFoldersAsync()
    {
        if (SelectedWorkspace == null) return;

        ApplyWorkspaceTargetFoldersFromText();
        _dbContext.UpdateEntity(SelectedWorkspace);
        await _dbContext.SaveChangesAsync();

        StatusMessage = $"Workspace folders updated: {string.Join(", ", SelectedWorkspace.TargetFolders)}";
    }

    public async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            await _dbContext.InitializeDatabaseAsync();

            var settings = _settingsService.GetSettings();
            M365ClientId = settings.M365ClientId;
            M365TenantId = string.IsNullOrWhiteSpace(settings.M365TenantId) ? "common" : settings.M365TenantId;
            M365RedirectUri = string.IsNullOrWhiteSpace(settings.M365RedirectUri) ? "http://localhost" : settings.M365RedirectUri;
            DatabasePath = settings.DatabasePath;
            DatabaseProvider = "SQLite (Embedded / Portable / WAL Mode)";
            AiProvider = settings.AiProvider;

            await UpdateAuthStatusAsync();

            Workspaces.Clear();
            var wsList = await _dbContext.Workspaces.ToListAsync();
            if (wsList.Count == 0)
            {
                var defaultWs = new Workspace
                {
                    Name = "Primary Workspace",
                    Description = "Default Evidence Management Workspace"
                };
                _dbContext.AddEntity(defaultWs);
                await _dbContext.SaveChangesAsync();
                wsList.Add(defaultWs);
            }

            foreach (var ws in wsList)
            {
                Workspaces.Add(ws);
            }

            SelectedWorkspace = Workspaces.FirstOrDefault();
            await LoadMattersAndCommunicationsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UpdateAuthStatusAsync()
    {
        var isAuth = await _authService.IsAuthenticatedAsync();
        IsM365Authenticated = isAuth;
        if (isAuth)
        {
            var email = await _authService.GetUserEmailAsync();
            M365AccountStatus = $"Connected ({email ?? "Active Session"})";
        }
        else
        {
            M365AccountStatus = string.IsNullOrWhiteSpace(M365ClientId)
                ? "Not Configured (Enter Client ID & Tenant ID below)"
                : "Not Connected (Click 'Sign In / Test Connection' to authenticate)";
        }
    }

    private async Task ExecuteSaveSettingsAsync()
    {
        try
        {
            var settings = new AppSettings
            {
                M365ClientId = M365ClientId?.Trim() ?? string.Empty,
                M365TenantId = string.IsNullOrWhiteSpace(M365TenantId) ? "common" : M365TenantId.Trim(),
                M365RedirectUri = string.IsNullOrWhiteSpace(M365RedirectUri) ? "http://localhost" : M365RedirectUri.Trim(),
                DatabasePath = DatabasePath,
                DatabaseProvider = "SQLite",
                AiProvider = AiProvider
            };

            await _settingsService.SaveSettingsAsync(settings);
            _authService.Configure(settings.M365ClientId, settings.M365TenantId);

            SettingsStatusMessage = "Settings successfully saved and applied.";
            StatusMessage = "Settings saved | Ready";
            await UpdateAuthStatusAsync();
        }
        catch (Exception ex)
        {
            SettingsStatusMessage = $"Error saving settings: {ex.Message}";
        }
    }

    private async Task ExecuteTestM365ConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(M365ClientId))
        {
            SettingsStatusMessage = "Please enter a valid Client ID before connecting.";
            return;
        }

        IsBusy = true;
        SettingsStatusMessage = "Connecting to Microsoft 365...";
        try
        {
            _authService.Configure(M365ClientId.Trim(), M365TenantId.Trim());
            var success = await _authService.SignInAsync();
            if (success)
            {
                SettingsStatusMessage = "Successfully connected to Microsoft 365!";
                StatusMessage = "M365 Connected | Ready";
            }
            else
            {
                SettingsStatusMessage = "Connection failed or was canceled. Please check your Client ID and Tenant ID.";
            }
            await UpdateAuthStatusAsync();
        }
        catch (Exception ex)
        {
            SettingsStatusMessage = $"Connection failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteSignOutM365Async()
    {
        IsBusy = true;
        try
        {
            await _authService.SignOutAsync();
            SettingsStatusMessage = "Signed out of Microsoft 365.";
            await UpdateAuthStatusAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadMattersAndCommunicationsAsync()
    {
        if (SelectedWorkspace == null) return;

        Matters.Clear();
        Communications.Clear();
        Claims.Clear();
        Relationships.Clear();

        var matters = await _dbContext.Matters.Where(m => m.WorkspaceId == SelectedWorkspace.Id).ToListAsync();
        foreach (var m in matters) Matters.Add(m);

        var comms = await _dbContext.Communications.Where(c => c.WorkspaceId == SelectedWorkspace.Id).ToListAsync();
        foreach (var c in comms) Communications.Add(c);

        var claims = await _dbContext.Claims.Where(cl => cl.WorkspaceId == SelectedWorkspace.Id).ToListAsync();
        foreach (var cl in claims) Claims.Add(cl);

        var rels = await _dbContext.ClaimRelationships.ToListAsync();
        foreach (var r in rels) Relationships.Add(r);

        TotalCommunicationsCount = Communications.Count;
        TotalClaimsCount = Claims.Count;
        ContradictionsCount = Relationships.Count(r => r.RelationshipType == Domain.Enums.ClaimRelationshipType.Contradicts);

        if (SelectedCommunication == null && Communications.Count > 0)
        {
            SelectedCommunication = Communications.First();
        }
        else
        {
            await UpdateSelectedCommunicationIntelligenceAsync();
        }
    }

    private async Task UpdateSelectedCommunicationIntelligenceAsync()
    {
        SelectedCommunicationClaims.Clear();
        SelectedCommunicationRelationships.Clear();

        if (SelectedCommunication == null)
        {
            return;
        }

        var commId = SelectedCommunication.Id;
        var conversationId = SelectedCommunication.ConversationId;

        // 1. Find all claims associated with this communication (via Statements or direct EvidenceLinks or Conversation)
        // Check statements belonging to this communication or conversation
        List<Guid> relevantCommIds = new() { commId };
        if (conversationId.HasValue)
        {
            var siblingCommIds = await _dbContext.Communications
                .Where(c => c.ConversationId == conversationId.Value)
                .Select(c => c.Id)
                .ToListAsync();
            relevantCommIds = relevantCommIds.Union(siblingCommIds).ToList();
        }

        var claimIdsFromStatements = await _dbContext.Statements
            .Where(s => relevantCommIds.Contains(s.CommunicationId) && s.ClaimId.HasValue)
            .Select(s => s.ClaimId!.Value)
            .Distinct()
            .ToListAsync();

        var claimIdsFromEvidence = await _dbContext.EvidenceLinks
            .Where(e => relevantCommIds.Contains(e.SourceCommunicationId) && e.TargetEntityType == "Claim")
            .Select(e => e.TargetEntityId)
            .Distinct()
            .ToListAsync();

        var allRelevantClaimIds = claimIdsFromStatements.Union(claimIdsFromEvidence).Distinct().ToList();

        var itemClaims = await _dbContext.Claims
            .Where(cl => allRelevantClaimIds.Contains(cl.Id))
            .ToListAsync();

        // If no explicit statements/evidence linked yet (e.g. claims matching subject/sender), fallback or list existing claims
        foreach (var claim in itemClaims)
        {
            SelectedCommunicationClaims.Add(claim);
        }

        if (allRelevantClaimIds.Count > 0)
        {
            var itemRels = await _dbContext.ClaimRelationships
                .Include(r => r.SourceClaim)
                .Include(r => r.TargetClaim)
                .Where(r => allRelevantClaimIds.Contains(r.SourceClaimId) || allRelevantClaimIds.Contains(r.TargetClaimId))
                .ToListAsync();

            foreach (var rel in itemRels)
            {
                SelectedCommunicationRelationships.Add(rel);
            }
        }
    }

    private async Task ExecuteSyncMailboxAsync()
    {
        if (SelectedWorkspace == null) return;

        if (string.IsNullOrWhiteSpace(_authService.ClientId))
        {
            StatusMessage = "Microsoft 365 Client ID is not configured. Switching to Settings...";
            SelectedTabIndex = 3;
            SettingsStatusMessage = "Please configure your Microsoft 365 Client ID and Tenant ID first.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Synchronizing mailbox with Microsoft 365...";
        try
        {
            var progress = new Progress<SyncProgressDto>(p =>
            {
                StatusMessage = $"{p.StatusMessage} ({p.ProcessedItems}/{p.TotalItems})";
                if (!string.IsNullOrEmpty(p.ErrorMessage))
                {
                    StatusMessage = $"Sync Error: {p.ErrorMessage}";
                }
            });

            await _syncService.SyncMailboxAsync(SelectedWorkspace.Id, SelectedMatter?.Id, progress);
            await LoadMattersAndCommunicationsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateDefaultWorkspaceAsync()
    {
        var newWs = new Workspace
        {
            Name = $"Workspace {Workspaces.Count + 1}",
            Description = "Communication intelligence case workspace"
        };
        _dbContext.AddEntity(newWs);
        await _dbContext.SaveChangesAsync();
        Workspaces.Add(newWs);
        SelectedWorkspace = newWs;
    }

    private async Task ExecuteDeleteWorkspaceAsync(Workspace? workspace)
    {
        await DeleteWorkspaceInternalAsync(workspace, promptUser: true);
    }

    public async Task DeleteWorkspaceInternalAsync(Workspace? workspace, bool promptUser = true)
    {
        var targetWorkspace = workspace ?? SelectedWorkspace;
        if (targetWorkspace == null) return;

        if (promptUser && System.Windows.Application.Current != null)
        {
            var confirmResult = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete workspace '{targetWorkspace.Name}'?\n\nThis will permanently delete this workspace and all its associated communications, claims, and matters.",
                "Confirm Workspace Deletion",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirmResult != System.Windows.MessageBoxResult.Yes)
            {
                return;
            }
        }

        IsBusy = true;
        StatusMessage = $"Deleting workspace '{targetWorkspace.Name}'...";
        try
        {
            await _dbContext.DeleteWorkspaceAsync(targetWorkspace.Id);
            Workspaces.Remove(targetWorkspace);

            if (SelectedWorkspace?.Id == targetWorkspace.Id)
            {
                SelectedWorkspace = Workspaces.FirstOrDefault();
                if (SelectedWorkspace == null)
                {
                    var defaultWs = new Workspace
                    {
                        Name = "Primary Workspace",
                        Description = "Default Evidence Management Workspace"
                    };
                    _dbContext.AddEntity(defaultWs);
                    await _dbContext.SaveChangesAsync();
                    Workspaces.Add(defaultWs);
                    SelectedWorkspace = defaultWs;
                }
            }

            await LoadMattersAndCommunicationsAsync();
            StatusMessage = $"Workspace '{targetWorkspace.Name}' deleted.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting workspace: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecutePurgeDatabaseAsync()
    {
        await PurgeDatabaseInternalAsync(promptUser: true);
    }

    public async Task PurgeDatabaseInternalAsync(bool promptUser = true)
    {
        if (promptUser && System.Windows.Application.Current != null)
        {
            var confirmResult = System.Windows.MessageBox.Show(
                "Are you sure you want to purge the database?\n\nThis will permanently delete all communications, extracted claims, conversations, matters, and intelligence analysis records.",
                "Confirm Database Purge",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirmResult != System.Windows.MessageBoxResult.Yes)
            {
                return;
            }
        }

        IsBusy = true;
        StatusMessage = "Purging database records...";
        try
        {
            await _dbContext.PurgeAllDataAsync();

            SelectedCommunication = null;
            SelectedMatter = null;
            SelectedWorkspace = null;
            Workspaces.Clear();
            Matters.Clear();
            Communications.Clear();
            Claims.Clear();
            Relationships.Clear();
            TotalCommunicationsCount = 0;
            TotalClaimsCount = 0;
            ContradictionsCount = 0;

            await LoadDataAsync();

            SettingsStatusMessage = "Database successfully purged. A fresh workspace has been initialized.";
            StatusMessage = "Database purged | Ready";
        }
        catch (Exception ex)
        {
            SettingsStatusMessage = $"Error purging database: {ex.Message}";
            StatusMessage = $"Database purge failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
