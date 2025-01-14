using Hangfire;
using Lingarr.Core;
using Lingarr.Core.Data;
using Lingarr.Server.Filters;
using Lingarr.Server.Hubs;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Extensions;

public static class ApplicationBuilderExtensions
{
    public static void Configure(this WebApplication app)
    {
        app.MapHubs();
        app.ApplyMigrations();

        if (app.Environment.IsDevelopment())
        {
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new LingarrAuthorizationFilter()]
            });
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint($"/swagger/{LingarrVersion.Number}/swagger.json",
                    $"Lingarr HTTP API {LingarrVersion.Number}");
                options.EnableTryItOutByDefault();
            });
        }

        app.UseAuthorization();
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

    private static void ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<LingarrDbContext>();
            Console.WriteLine("Applying migrations...");
            context.Database.Migrate();
            Console.WriteLine("Migrations applied successfully.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"An error occurred while applying migrations. {ex}", ex);
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