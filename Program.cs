using FX5u_Web_HMI_App;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FX5u_Web_HMI_App.Hubs;
using FX5u_Web_HMI_App.BackgroundServices;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using FX5u_Web_HMI_App.Data;
// 1. ADD THIS NAMESPACE FOR LOCALIZATION VIEW EXPANDER
using Microsoft.AspNetCore.Mvc.Razor;

var builder = WebApplication.CreateBuilder(args);

// --- 2. CONFIGURE LOCALIZATION OPTIONS ---
var supportedCultures = new[]
{
    new CultureInfo("en-US"), // English (United States)
    new CultureInfo("gu-IN")  // Gujarati (India)
};

// This registers IStringLocalizerFactory
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});
builder.Services.AddSingleton<FX5u_Web_HMI_App.PageStateTracker>();

// --- 3. ADD RAZOR PAGES WITH VIEW LOCALIZATION (CRITICAL FIX) ---
builder.Services.AddRazorPages()
    // This line registers IViewLocalizer (Fixes your crash)
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    // This allows localizing error messages in Models
    .AddDataAnnotationsLocalization();

// Add Database Context
builder.Services.AddDbContext<LogDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HMI Services
builder.Services.AddSingleton<ISLMPService, SLMPService>();
builder.Services.AddSignalR();
builder.Services.AddHostedService<PlcMonitorService>();

// If you are using the Safety/Distance check we discussed, enable this:
// builder.Services.AddSingleton<FX5u_Web_HMI_App.Services.SafetyService>(); 

var app = builder.Build();

// --- 4. CONFIGURE THE HTTP REQUEST PIPELINE ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// --- 5. ADD LOCALIZATION MIDDLEWARE ---
// Must be AFTER UseRouting() and BEFORE UseAuthorization()
var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>();
app.UseRequestLocalization(locOptions.Value);

app.UseAuthorization();

app.MapRazorPages();
app.MapHub<PlcHub>("/plcHub");

// --- 6. AUTOMATICALLY CREATE/UPDATE THE DATABASE ON STARTUP ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<LogDbContext>();
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();