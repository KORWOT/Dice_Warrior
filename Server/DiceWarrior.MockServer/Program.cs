using DiceWarrior.MockServer;
using DiceWarrior.Metrics;

var builder=WebApplication.CreateBuilder(args);
if(!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("This Mock server only runs with --environment Development. Production identity is not configured.");
var settings=MockServerSettings.Read(builder.Configuration);
builder.WebHost.UseUrls(settings.Url);
builder.WebHost.ConfigureKestrel(options=>options.Limits.MaxRequestBodySize=512*1024);
// Do not log account headers, URLs containing operation IDs, or request/response bodies.
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options=>options.SingleLine=true);
builder.Logging.SetMinimumLevel(LogLevel.Warning);
var app=builder.Build();
var metrics=new MetricsCollector(builder.Configuration["Metrics:Directory"]??Path.Combine(Path.GetDirectoryName(settings.DatabasePath)!,"metrics"),settings.DatabasePath);
AuthorityEndpoints.Map(app,settings,metrics);
MetricsEndpoints.Map(app,metrics);
Console.WriteLine($"Dice Warrior development Mock ready: {settings.Url}; SQLite WAL; protocol 1. No Firebase connection.");
app.Run();
