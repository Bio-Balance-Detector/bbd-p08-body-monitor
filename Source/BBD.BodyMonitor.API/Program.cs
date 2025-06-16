/// <remarks>
/// Main entry point for the BBD.BodyMonitor.API application.
/// This file is responsible for:
/// - Configuring and building the ASP.NET Core web application host.
/// - Setting up logging providers with custom formatting.
/// - Processing command-line arguments and user secrets for configuration.
/// - Binding configuration values to the <see cref="BBD.BodyMonitor.Configuration.BodyMonitorOptions"/> class.
/// - Registering essential services for data processing (<see cref="BBD.BodyMonitor.Services.IDataProcessorService"/>)
///   and session management (<see cref="BBD.BodyMonitor.Services.ISessionManagerService"/>) in the dependency injection container.
/// - Configuring controllers, API explorer, and Swagger/OpenAPI for API documentation and testing.
/// - Displaying a welcome screen with the application version.
/// - Implementing a mutex to ensure only one instance of the application runs at a time.
/// - Setting up the HTTP request pipeline, including Swagger UI for development, HTTPS redirection,
///   authorization, controller mapping, and WebSocket support.
/// - Running the web application.
/// </remarks>
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

app.Logger.LogInformation($"Bio Balance Detector Body Monitor API v{versionString}");
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

app.UseWebSockets();

app.Run();
