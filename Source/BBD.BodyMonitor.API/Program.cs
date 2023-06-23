using BBD.BodyMonitor;
using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Services;
using Microsoft.Extensions.Logging.Console;

Mutex mutex = new(true, "{79bb7f72-37bc-41ff-9014-ed8662659b52}");

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsoleFormatter<CustomSimpleConsoleFormatter, SimpleConsoleFormatterOptions>();
builder.Logging.AddConsole(options => options.FormatterName = "customSimpleConsoleFormatter");
builder.Logging.AddDebug();

// Process command line arguments
Dictionary<string, string> argsMappings = new()
{
                { "--datadirectory", "DataDirectory" },
                { "--samplerate", "Acquisition:Samplerate" }
            };

// Build configuration
builder.Configuration.AddUserSecrets<Program>(optional: true)
.AddCommandLine(args, argsMappings);

builder.Services.AddOptions<BodyMonitorOptions>()
            .Bind(builder.Configuration.GetSection("BodyMonitor"))
            .ValidateDataAnnotations();

// Add services to the container.
builder.Services.AddSingleton<IDataProcessorService, DataProcessorService>();
builder.Services.AddSingleton<ISessionManagerService, SessionManagerService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Fitbit OAuth 2.0 authentication
//builder.Services.AddAuthentication().AddOAuth()

WebApplication app = builder.Build();

string versionString = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
DataProcessorService.ShowWelcomeScreen(versionString);

app.Logger.LogInformation($"Bio Balance Detector Body Monitor v{versionString}");
app.Logger.LogInformation($"(The current UTC time is {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss})");


if (!mutex.WaitOne(TimeSpan.Zero, true))
{
    app.Logger.LogError($"An instance of this app is already running on this machine.");
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
