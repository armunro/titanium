using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Titanium.Application.Interfaces;
using Titanium.Infrastructure.Configuration;
using Titanium.Infrastructure.Persistence;

namespace Titanium.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string dbPath)
    {
        services.AddSingleton<IAppSettingsService>(sp => new AppSettingsService());

        services.AddDbContext<TitaniumDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dbPath};Cache=Shared");
        });

        services.AddScoped<ITitaniumDbContext>(sp => sp.GetRequiredService<TitaniumDbContext>());
        services.AddScoped<IBackgroundJobQueue, BackgroundJobQueue>();

        return services;
    }
}
