using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Titanium.Application.Interfaces;

namespace Titanium.Integrations.M365.Authentication;

public class M365AuthenticationService : IM365AuthenticationService
{
    private static readonly string[] Scopes = new[] { "User.Read", "Mail.Read", "offline_access" };
    private readonly IAppSettingsService? _settingsService;
    private IPublicClientApplication? _pca;
    private string _clientId = string.Empty;
    private string _tenantId = "common";
    private string? _cachedUserEmail;

    public string ClientId => _clientId;
    public string TenantId => _tenantId;

    public M365AuthenticationService(IAppSettingsService? settingsService = null)
    {
        _settingsService = settingsService;
        if (_settingsService != null)
        {
            var settings = _settingsService.GetSettings();
            _clientId = settings.M365ClientId;
            _tenantId = string.IsNullOrWhiteSpace(settings.M365TenantId) ? "common" : settings.M365TenantId;
        }
    }

    public void Configure(string clientId, string tenantId = "common")
    {
        _clientId = clientId?.Trim() ?? string.Empty;
        _tenantId = string.IsNullOrWhiteSpace(tenantId) ? "common" : tenantId.Trim();
        _pca = null;

        if (_settingsService != null)
        {
            var settings = _settingsService.GetSettings();
            settings.M365ClientId = _clientId;
            settings.M365TenantId = _tenantId;
            _ = _settingsService.SaveSettingsAsync(settings);
        }
    }

    private async Task<IPublicClientApplication> GetClientAppAsync()
    {
        if (_pca != null) return _pca;

        if (string.IsNullOrWhiteSpace(_clientId))
        {
            throw new InvalidOperationException("Microsoft 365 Client ID is not configured. Please enter your Client ID in Settings.");
        }

        var builder = PublicClientApplicationBuilder.Create(_clientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, _tenantId)
            .WithRedirectUri("http://localhost");

        _pca = builder.Build();

        try
        {
            var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Titanium", "MsalCache");
            var storageCreationProperties = new StorageCreationPropertiesBuilder("msal.cache", cacheDir)
                .WithMacKeyChain("TitaniumKeyChain", "TitaniumService")
                .WithLinuxKeyring("com.titanium.app", "default", "TitaniumKeyring", new KeyValuePair<string, string>("app", "titanium"), new KeyValuePair<string, string>("account", "user"))
                .Build();

            var cacheHelper = await MsalCacheHelper.CreateAsync(storageCreationProperties);
            cacheHelper.RegisterCache(_pca.UserTokenCache);
        }
        catch
        {
            // Fallback for dev/test environments without keychain
        }

        return _pca;
    }

    public async Task<bool> SignInAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var pca = await GetClientAppAsync();
            var accounts = await pca.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();

            AuthenticationResult result;
            if (firstAccount != null)
            {
                try
                {
                    result = await pca.AcquireTokenSilent(Scopes, firstAccount).ExecuteAsync(cancellationToken);
                    _cachedUserEmail = result.Account.Username;
                    return true;
                }
                catch (MsalUiRequiredException)
                {
                    // Silent failed, proceed to interactive
                }
            }

            result = await pca.AcquireTokenInteractive(Scopes)
                .WithPrompt(Prompt.SelectAccount)
                .ExecuteAsync(cancellationToken);

            _cachedUserEmail = result.Account.Username;
            return result.AccessToken != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        var pca = await GetClientAppAsync();
        var accounts = await pca.GetAccountsAsync();
        foreach (var account in accounts)
        {
            await pca.RemoveAsync(account);
        }
        _cachedUserEmail = null;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var token = await GetAccessTokenAsync();
            return !string.IsNullOrEmpty(token);
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var pca = await GetClientAppAsync();
            var accounts = await pca.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();
            if (firstAccount == null) return null;

            var result = await pca.AcquireTokenSilent(Scopes, firstAccount).ExecuteAsync(cancellationToken);
            _cachedUserEmail = result.Account.Username;
            return result.AccessToken;
        }
        catch
        {
            return null;
        }
    }

    public Task<string?> GetUserEmailAsync()
    {
        return Task.FromResult(_cachedUserEmail);
    }
}
