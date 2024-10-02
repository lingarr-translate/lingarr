using Hangfire;
using Hangfire.Storage.SQLite;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Server.Hubs;
using Lingarr.Server.Interfaces.Providers;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Integration;
using Lingarr.Server.Interfaces.Services.Subtitle;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Listeners;
using Lingarr.Server.Providers;
using Lingarr.Server.Services;
using Lingarr.Server.Services.Integration;
using Lingarr.Server.Services.Subtitle;
using Lingarr.Server.Services.Translation;

namespace Lingarr.Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static void Configure(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler =
                System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddMemoryCache();
        builder.Services.AddHttpClient();

        builder.ConfigureDatabase();
        builder.ConfigureProviders();
        builder.ConfigureServices();
        builder.ConfigureHangfire();
        builder.ConfigureSignalR();
    }

    private static void ConfigureDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<LingarrDbContext>(options =>
        {
            DatabaseConfiguration.ConfigureDbContext(options);
        });
    }

    private static void ConfigureProviders(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IIntegrationSettingsProvider, IntegrationSettingsProvider>();
    }

    private static void ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<SettingService>(); 
        builder.Services.AddScoped<ISettingService, SettingService>();
        builder.Services.AddScoped<SettingChangeListener>();
        builder.Services.AddScoped<ISettingService>(serviceProvider =>
        {
            var settingsService = serviceProvider.GetRequiredService<SettingService>();
            var someService = serviceProvider.GetRequiredService<SettingChangeListener>();
    
            settingsService.RegisterHandler(someService);
    
            return settingsService;
        });

        builder.Services.AddScoped<IImageService, ImageService>();
        builder.Services.AddScoped<IIntegrationService, IntegrationService>();
        builder.Services.AddScoped<IMediaService, MediaService>();
        builder.Services.AddScoped<IProgressService, ProgressService>();
        builder.Services.AddScoped<IRadarrService, RadarrService>();
        builder.Services.AddHostedService<ScheduleInitializationService>();
        builder.Services.AddSingleton<IScheduleService, ScheduleService>();
        builder.Services.AddScoped<ISonarrService, SonarrService>();
        builder.Services.AddScoped<ISubtitleService, SubtitleService>();
        builder.Services.AddScoped<MediaSubtitleProcessor>();

        builder.Services.AddScoped<ISubRipParser, SubRipParser>();
        builder.Services.AddScoped<ISubRipWriter, SubRipWriter>();

        builder.Services.AddScoped<ITranslationServiceFactory, TranslationServiceFactory>();
    }

    private static void ConfigureHangfire(this WebApplicationBuilder builder)
    {
        builder.Services.AddHangfireServer(options =>
        {
            options.Queues = ["movies", "shows", "default"];
            options.WorkerCount =
                int.TryParse(Environment.GetEnvironmentVariable("MAX_CONCURRENT_JOBS"), out int maxConcurrentJobs)
                    ? maxConcurrentJobs
                    : 1;
        });

        builder.Services.AddHangfire(configuration => configuration
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSQLiteStorage());
    }

    private static void ConfigureSignalR(this WebApplicationBuilder builder)
    {
        builder.Services.AddSignalR().AddHubOptions<ScheduleProgressHub>(options =>
        {
            options.EnableDetailedErrors = true;
        });
    }
}