using Hangfire;
using Lingarr.Core;
using Lingarr.Migrations;
using Lingarr.Server.Hubs;

namespace Lingarr.Server.Extensions;

public static class ApplicationBuilderExtensions
{
    public static async Task Configure(this WebApplication app)
    {
        app.MapHubs();
        await app.ApplyMigrations();

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