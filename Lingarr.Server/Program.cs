using Lingarr.Server.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Configure();

var app = builder.Build();
await app.Configure();
app.Run();