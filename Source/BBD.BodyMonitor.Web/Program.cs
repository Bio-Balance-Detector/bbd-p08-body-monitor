// This is the main entry point for the BBD.BodyMonitor.Web application.
// It sets up the web application builder, configures services and middleware,
// and runs the application.
using BBD.BodyMonitor.Web.Data;

// Mutex to ensure only one instance of the application is running.
Mutex mutex = new(true, "{02fec892-cb10-47ca-b7c8-2b6f016c85ac}");

// Create a new web application builder.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the dependency injection container.
builder.Services.AddRazorPages(); // Adds support for Razor Pages.
builder.Services.AddServerSideBlazor(); // Adds support for Server-Side Blazor.
builder.Services.AddSingleton<BioBalanceDetectorService>(); // Registers the BioBalanceDetectorService as a singleton.

// Build the web application.
WebApplication app = builder.Build();

// Get application version and log it.
string versionString = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
app.Logger.LogInformation($"Bio Balance Detector Body Monitor UI v{versionString}");
app.Logger.LogInformation($"(The current UTC time is {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss})");

// Check if another instance of the application is already running.
if (!mutex.WaitOne(TimeSpan.Zero, true))
{
    app.Logger.LogError($"An instance of this app is already running on this machine.");
    // Exit if another instance is running.
    return;
}

// Configure the HTTP request pipeline.
// This section defines how the application responds to HTTP requests.
if (!app.Environment.IsDevelopment())
{
    // Use a custom error handler page in non-development environments.
    _ = app.UseExceptionHandler("/Error");
    // Use HTTP Strict Transport Security (HSTS) for enhanced security.
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    _ = app.UseHsts();
}

// Redirect HTTP requests to HTTPS.
app.UseHttpsRedirection();

// Enable serving static files (e.g., CSS, JavaScript, images).
app.UseStaticFiles();

// Enable routing.
app.UseRouting();

// Map Blazor Hub for real-time communication.
app.MapBlazorHub();
// Map a fallback page for unmatched routes.
app.MapFallbackToPage("/_Host");

// Run the application.
// This starts the web server and listens for incoming requests.
app.Run();
