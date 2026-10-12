using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Titanium.Application.Interfaces;
using Titanium.Application.Services;
using Titanium.Desktop.ViewModels;
using Titanium.Infrastructure;
using Titanium.Integrations.M365.Authentication;
using Titanium.Integrations.M365.Synchronization;
using Titanium.Intelligence.Classification;
using Titanium.Intelligence.Contradictions;
using Titanium.Intelligence.Extraction;
using Titanium.Intelligence.Retrieval;
using Titanium.Workers.Processing;

namespace Titanium.Desktop;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Titanium");
        Directory.CreateDirectory(appData);
        var dbPath = Path.Combine(appData, "titanium.db");

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Infrastructure
                services.AddInfrastructureServices(dbPath);

                // Application Services
                services.AddScoped<ICommunicationNormalizer, CommunicationNormalizer>();
                services.AddScoped<EvidenceAnalysisOrchestrator>();
                services.AddScoped<MatterManagementService>();

                // Microsoft 365
                services.AddSingleton<IM365AuthenticationService, M365AuthenticationService>();
                services.AddScoped<IM365MailboxSyncService, M365MailboxSyncService>();

                // Intelligence
                services.AddScoped<IStatementExtractor, RuleBasedStatementExtractor>();
                services.AddScoped<IClaimNormalizer, RuleBasedClaimNormalizer>();
                services.AddScoped<IRelevantClaimRetriever, CandidateClaimRetriever>();
                services.AddScoped<IClaimRelationshipAnalyzer, EvidenceBasedClaimRelationshipAnalyzer>();

                // Background Worker
                services.AddHostedService<TitaniumBackgroundWorker>();

                // ViewModels
                services.AddTransient<MainViewModel>();
                services.AddTransient<MainWindow>();
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
            })
            .Build();

        await _host.StartAsync();

        // Ensure database is initialized
        using (var scope = _host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Titanium.Infrastructure.Persistence.TitaniumDbContext>();
            await db.InitializeDatabaseAsync();
        }

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
