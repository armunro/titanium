using Titanium.Application.Interfaces;
using Titanium.Infrastructure.Configuration;
using Xunit;

namespace Titanium.Application.Tests;

public class AppSettingsServiceTests : IDisposable
{
    private readonly string _tempSettingsPath;

    public AppSettingsServiceTests()
    {
        _tempSettingsPath = Path.Combine(Path.GetTempPath(), $"titanium_settings_test_{Guid.NewGuid()}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempSettingsPath))
        {
            try { File.Delete(_tempSettingsPath); } catch { }
        }
    }

    [Fact]
    public async Task AppSettingsService_SaveAndLoad_ShouldPersistCorrectly()
    {
        // Arrange
        var service = new AppSettingsService(_tempSettingsPath);
        var expectedSettings = new AppSettings
        {
            M365ClientId = "12345678-abcd-1234-abcd-1234567890ab",
            M365TenantId = "organizations",
            M365RedirectUri = "http://localhost",
            DatabasePath = "C:\\Data\\titanium.db",
            DatabaseProvider = "SQLite",
            AiProvider = "Local / Rule-based"
        };

        // Act
        await service.SaveSettingsAsync(expectedSettings);

        var newServiceInstance = new AppSettingsService(_tempSettingsPath);
        var loaded = await newServiceInstance.LoadSettingsAsync();

        // Assert
        Assert.Equal(expectedSettings.M365ClientId, loaded.M365ClientId);
        Assert.Equal(expectedSettings.M365TenantId, loaded.M365TenantId);
        Assert.Equal(expectedSettings.M365RedirectUri, loaded.M365RedirectUri);
        Assert.Equal(expectedSettings.DatabasePath, loaded.DatabasePath);
        Assert.Equal("SQLite", loaded.DatabaseProvider);
    }

    [Fact]
    public void AppSettingsService_DefaultSettings_ShouldHaveSqliteAndCommonTenant()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.json");
        var service = new AppSettingsService(nonExistentPath);

        // Act
        var settings = service.GetSettings();

        // Assert
        Assert.Equal("common", settings.M365TenantId);
        Assert.Equal("SQLite", settings.DatabaseProvider);
        Assert.Contains("titanium.db", settings.DatabasePath);
    }
}
