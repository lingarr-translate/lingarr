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
        var basePath = (Environment.GetEnvironmentVariable("BASE_PATH") ?? "").TrimEnd('/');
        if (!string.IsNullOrEmpty(basePath))
        {
            app.UsePathBase(basePath);
        }

        app.UseRouting();

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
        app.ConfigureSpa(basePath);
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
        using var scope = app.Services.CreateScope();
        var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

        foreach (var key in SettingKeys.EncryptedKeys)
        {
            var value = await settingService.GetSetting(key);
            if (string.IsNullOrEmpty(value)) continue;

            if (!encryptionService.IsEncrypted(value))
            {
                await settingService.SetEncryptedSetting(key, value);
                Console.WriteLine($"Migrated '{key}' to encrypted storage.");
            }
        }
    }

    private static void ConfigureSpa(this WebApplication app, string basePath)
    {
        app.MapWhen(httpContext =>
                httpContext.Request.Path.Value != null &&
                !httpContext.Request.Path.Value.StartsWith("/api") &&
                !httpContext.Request.Path.Value.StartsWith("/signalr"),
            configBuilder =>
            {
                var index = File.ReadAllText(Path.Combine(app.Environment.WebRootPath, "index.html"));
                index = index.Replace("<head>",
                    $"<head><base href=\"{basePath}/\">" +
                    $"<meta name=\"lingarr-timezone\" content=\"{TimeZoneConfiguration.IanaId}\">");

                configBuilder.Run(async ctx =>
                {
                    ctx.Response.ContentType = "text/html";
                    await ctx.Response.WriteAsync(index);
                });
            });
    }
}