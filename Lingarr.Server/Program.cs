using Lingarr.Core.Configuration;
using Lingarr.Server.Extensions;

EnvironmentFileConfiguration.Apply();

var builder = WebApplication.CreateBuilder(args);
builder.Configure();

var app = builder.Build();
await app.Configure();
app.Run();