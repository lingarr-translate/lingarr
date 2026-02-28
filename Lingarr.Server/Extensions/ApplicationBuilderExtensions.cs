using Hangfire;
using Lingarr.Core;
using Lingarr.Core.Configuration;
using Lingarr.Migrations;
using Lingarr.Server.Hubs;
using Lingarr.Server.Interfaces.Services;

namespace Lingarr.Server.Extensions;

public static class ApplicationBuilderExtensions
{
    public static async Task Configure(this WebApplication app)
    {
        app.MapHubs();
        await app.ApplyMigrations();
        await app.MigrateApiKeyEncryption();

        if (app.Environment.IsDevelopment())
        {
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = []
            });
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint($"/swagger/{LingarrVersion.Number}/swagger.json",
                    $"Lingarr HTTP API {LingarrVersion.Number}");
                options.EnableTryItOutByDefault();
            });
        }

        app.UseAuthentication();
        app.MapControllers();
        app.UseStaticFiles();
        app.ConfigureSpa();
    }

    private static void MapHubs(this WebApplication app)
    {
        app.MapHub<TranslationRequestsHub>("/signalr/TranslationRequests");
        app.MapHub<SettingUpdatesHub>("/signalr/SettingUpdates");
        app.MapHub<JobProgressHub>("/signalr/JobProgress");
    }

    private static Task ApplyMigrations(this WebApplication app)
    {
        try
        {
            Console.WriteLine("Applying migrations...");
            MigrationConfiguration.RunMigrations(app.Services);
            Console.WriteLine("Migrations applied successfully.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"An error occurred while applying migrations: {ex}");
            throw;
        }

        return Task.CompletedTask;
    }

    private static async Task MigrateApiKeyEncryption(this WebApplication app)
    {
        string[] apiKeys =
        [
            SettingKeys.Authentication.ApiKey,
            SettingKeys.Integration.RadarrApiKey,
            SettingKeys.Integration.SonarrApiKey,
            SettingKeys.Translation.OpenAi.ApiKey,
            SettingKeys.Translation.Anthropic.ApiKey,
            SettingKeys.Translation.Gemini.ApiKey,
            SettingKeys.Translation.DeepSeek.ApiKey,
            SettingKeys.Translation.DeepL.DeeplApiKey,
            SettingKeys.Translation.LibreTranslate.ApiKey,
            SettingKeys.Translation.LocalAi.ApiKey,
        ];

        using var scope = app.Services.CreateScope();
        var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

        foreach (var key in apiKeys)
        {
            var value = await settingService.GetSetting(key);
            if (string.IsNullOrEmpty(value)) continue;

            try
            {
                encryptionService.Decrypt(value);
            }
            catch
            {
                await settingService.SetEncryptedSetting(key, value);
                Console.WriteLine($"Migrated '{key}' to encrypted storage.");
            }
        }
    }

    private static void ConfigureSpa(this WebApplication app)
    {
        app.MapWhen(httpContext => 
                httpContext.Request.Path.Value != null && 
                !httpContext.Request.Path.Value.StartsWith("/api") && 
                !httpContext.Request.Path.Value.StartsWith("/signalr"),
            configBuilder =>
            {
                configBuilder.UseSpa(spa =>
                {
                    if (app.Environment.IsDevelopment())
                    {
                        spa.UseProxyToSpaDevelopmentServer("http://Lingarr.Client:9876");
                    }
                });
            });
    }
}