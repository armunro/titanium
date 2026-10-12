using System.Text.Json;
using Titanium.Application.Interfaces;

namespace Titanium.Infrastructure.Configuration;

public class AppSettingsService : IAppSettingsService
{
    private readonly string _settingsFilePath;
    private AppSettings _currentSettings;
    private readonly object _lock = new();

    public AppSettingsService(string? customSettingsPath = null)
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Titanium");
        Directory.CreateDirectory(appData);

        _settingsFilePath = customSettingsPath ?? Path.Combine(appData, "settings.json");
        _currentSettings = LoadSettingsInternal();
    }

    public AppSettings GetSettings()
    {
        lock (_lock)
        {
            return new AppSettings
            {
                M365ClientId = _currentSettings.M365ClientId,
                M365TenantId = _currentSettings.M365TenantId,
                M365RedirectUri = _currentSettings.M365RedirectUri,
                DatabasePath = _currentSettings.DatabasePath,
                DatabaseProvider = _currentSettings.DatabaseProvider,
                AiProvider = _currentSettings.AiProvider,
                OpenAiApiKey = _currentSettings.OpenAiApiKey
            };
        }
    }

    public Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _currentSettings = LoadSettingsInternal();
            return Task.FromResult(GetSettings());
        }
    }

    public async Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        string json;
        lock (_lock)
        {
            _currentSettings = new AppSettings
            {
                M365ClientId = settings.M365ClientId,
                M365TenantId = settings.M365TenantId,
                M365RedirectUri = settings.M365RedirectUri,
                DatabasePath = settings.DatabasePath,
                DatabaseProvider = settings.DatabaseProvider,
                AiProvider = settings.AiProvider,
                OpenAiApiKey = settings.OpenAiApiKey
            };
            json = JsonSerializer.Serialize(_currentSettings, new JsonSerializerOptions { WriteIndented = true });
        }

        await File.WriteAllTextAsync(_settingsFilePath, json, cancellationToken);
    }

    private AppSettings LoadSettingsInternal()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    if (string.IsNullOrWhiteSpace(loaded.DatabasePath))
                    {
                        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Titanium");
                        loaded.DatabasePath = Path.Combine(appData, "titanium.db");
                    }
                    return loaded;
                }
            }
        }
        catch
        {
            // Fallback to defaults
        }

        var defaultAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Titanium");
        return new AppSettings
        {
            M365ClientId = string.Empty,
            M365TenantId = "common",
            M365RedirectUri = "http://localhost",
            DatabasePath = Path.Combine(defaultAppData, "titanium.db"),
            DatabaseProvider = "SQLite",
            AiProvider = "Local / Rule-based"
        };
    }
}
