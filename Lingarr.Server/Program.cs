using Quartz;
using Quartz.Impl;
using Lingarr.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Register controllers
builder.Services.AddControllers();
// Add swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Register cache
builder.Services.AddMemoryCache();
// Register Http client
builder.Services.AddHttpClient();
// Register services
builder.Services.AddScoped<DirectoryService>();
builder.Services.AddScoped<LanguageService>();
builder.Services.AddScoped<SchedulerService>();
builder.Services.AddScoped<TranslateService>();

// Register scheduler
builder.Services.AddSingleton<IScheduler>(provider => StdSchedulerFactory.GetDefaultScheduler().Result);

var app = builder.Build();

// Configure development tools
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();
app.UseStaticFiles();


app.UseSpa(spa =>
{
    spa.Options.SourcePath = "ClientApp";
});

app.Run();
