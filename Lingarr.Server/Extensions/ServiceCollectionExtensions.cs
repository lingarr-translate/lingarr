using Hangfire;
using Hangfire.Storage.SQLite;
using Lingarr.Core;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Logging;
using Lingarr.Server.Hubs;
using Lingarr.Server.Interfaces.Providers;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Integration;
using Lingarr.Server.Interfaces.Services.Subtitle;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Listener;
using Lingarr.Server.Providers;
using Lingarr.Server.Services;
using Lingarr.Server.Services.Integration;
using Lingarr.Server.Services.Subtitle;
using Lingarr.Server.Services.Translation;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

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

        builder.Services.AddEndpointsApiExplorer();;
        builder.Services.AddMemoryCache();
        builder.Services.AddHttpClient();

        builder.ConfigureSwagger();
        builder.ConfigureLogging();
        builder.ConfigureDatabase();
        builder.ConfigureProviders();
        builder.ConfigureServices();
        builder.ConfigureHangfire();
        builder.ConfigureSignalR();
    }

    private static void ConfigureSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(LingarrVersion.Number, new OpenApiInfo
            {
                Title = "Lingarr HTTP API",
                Version = LingarrVersion.Number,
                Description = "Lingarr HTTP API definition",
                License = new OpenApiLicense
                {
                    Name = "GNU Affero General Public License v3.0",
                    Url = new Uri("https://github.com/lingarr-translate/lingarr/blob/main/LICENSE")
                }
            });
        });
    }

    private static void ConfigureLogging(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new CustomLogFormatter(Options.Create(new CustomLogFormatterOptions())));
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
        builder.Services.AddScoped<ISettingService, SettingService>();
        builder.Services.AddSingleton<SettingChangedListener>();

        builder.Services.AddHostedService<ScheduleInitializationService>();
        builder.Services.AddSingleton<IScheduleService, ScheduleService>();
        
        builder.Services.AddScoped<IImageService, ImageService>();
        builder.Services.AddScoped<IIntegrationService, IntegrationService>();
        builder.Services.AddScoped<IMediaService, MediaService>();
        builder.Services.AddScoped<IProgressService, ProgressService>();
        builder.Services.AddScoped<IRadarrService, RadarrService>();
        builder.Services.AddScoped<ISonarrService, SonarrService>();
        builder.Services.AddScoped<ISubtitleService, SubtitleService>();
        builder.Services.AddScoped<MediaSubtitleProcessor>();

        builder.Services.AddScoped<ISubRipParser, SubRipParser>();
        builder.Services.AddScoped<ISubRipWriter, SubRipWriter>();

        // Register translate services
        builder.Services.AddScoped<ITranslationServiceFactory, TranslationServiceFactory>();
        
        // Added startup service to validate new settings
        builder.Services.AddHostedService<StartupService>();
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