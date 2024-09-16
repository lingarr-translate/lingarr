using Lingarr.Server.Services;
using Lingarr.Server.Services.Subtitle;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.Storage.SQLite;
using Lingarr.Core.Data;
using Lingarr.Server.Filters;
using Lingarr.Server.Interfaces.Providers;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Subtitle;
using Lingarr.Server.Providers;

var builder = WebApplication.CreateBuilder(args);

// Register controllers
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
// Add swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Register cache
builder.Services.AddMemoryCache();
// Register Http client
builder.Services.AddHttpClient();
// Register DbContext
builder.Services.AddDbContext<LingarrDbContext>(options  =>
{
    var dbConnection = Environment.GetEnvironmentVariable("DB_CONNECTION")?.ToLower() ?? "sqlite";
    if (dbConnection == "mysql")
    {
        var variables = new Dictionary<string, string>
        {
            { "DB_HOST", Environment.GetEnvironmentVariable("DB_HOST") ?? "Lingarr.Mysql" },
            { "DB_PORT", Environment.GetEnvironmentVariable("DB_PORT") ?? "3306" },
            { "DB_DATABASE", Environment.GetEnvironmentVariable("DB_DATABASE") ?? "LingarrMysql" },
            { "DB_USERNAME", Environment.GetEnvironmentVariable("DB_USERNAME") ?? "LingarrMysql" },
            { "DB_PASSWORD", Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "Secret1234" }
        };

        var missingVariables = variables.Where(kv => string.IsNullOrEmpty(kv.Value)).Select(kv => kv.Key).ToList();
        if (missingVariables.Any())
        {
            throw new InvalidOperationException(
                $"MySQL connection environment variable(s) '{string.Join(", ", missingVariables)}' is missing or empty.");
        }

        var connectionString =
            $"Server={variables["DB_HOST"]};Port={variables["DB_PORT"]};Database={variables["DB_DATABASE"]};Uid={variables["DB_USERNAME"]};Pwd={variables["DB_PASSWORD"]};Allow User Variables=True";
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                mysqlOptions => mysqlOptions.MigrationsAssembly("Lingarr.Migrations.MySQL")
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .UseSnakeCaseNamingConvention();
    }
    else
    {
        var sqliteDbPath = Environment.GetEnvironmentVariable("SQLITE_DB_PATH") ?? "local.db";
        options.UseSqlite($"Data Source=/app/config/{sqliteDbPath}",
                sqliteOptions => sqliteOptions.MigrationsAssembly("Lingarr.Migrations.SQLite")
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .UseSnakeCaseNamingConvention();
    }
});
// Register Providers
builder.Services.AddScoped<ISonarrSettingsProvider, SonarrSettingsProvider>();
builder.Services.AddScoped<IRadarrSettingsProvider, RadarrSettingsProvider>();
// Register services
builder.Services.AddScoped<ITranslateService, TranslateService>();
builder.Services.AddScoped<ISubtitleService, SubtitleService>();
builder.Services.AddScoped<ISettingService, SettingService>();
builder.Services.AddScoped<IRadarrService, RadarrService>();
builder.Services.AddScoped<ISonarrService, SonarrService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<ISubRipParser, SubRipParser>();
builder.Services.AddScoped<ISubRipWriter, SubRipWriter>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddSingleton<IScheduleService, ScheduleService>();
builder.Services.AddHostedService<ScheduleInitializationService>();

builder.Services.AddHangfireServer(options =>
{
    options.Queues = ["movies", "shows", "default"];
    options.WorkerCount = 5;
});
// Add Hangfire scheduler services
builder.Services.AddHangfire(configuration => configuration
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSQLiteStorage());

var app = builder.Build();
// Apply migrations
using (var scope = app.Services.CreateScope())
{
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
        Console.Error.WriteLine("An error occurred while applying migrations. {ex}", ex);
    }
}

// Configure development tools
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new LingarrAuthorizationFilter()]
    });
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.UseStaticFiles();

app.MapWhen(httpContext => httpContext.Request.Path.Value != null && !httpContext.Request.Path.Value.StartsWith("/api"),
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

app.Run();
