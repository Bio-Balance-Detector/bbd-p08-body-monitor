using BBD.BodyMonitor.Web.Data;

Mutex mutex = new(true, "{02fec892-cb10-47ca-b7c8-2b6f016c85ac}");

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<BioBalanceDetectorService>();

WebApplication app = builder.Build();

string versionString = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();

app.Logger.LogInformation($"Bio Balance Detector Body Monitor UI v{versionString}");
app.Logger.LogInformation($"(The current UTC time is {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss})");


if (!mutex.WaitOne(TimeSpan.Zero, true))
{
    app.Logger.LogError($"An instance of this app is already running on this machine.");
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    _ = app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    _ = app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
