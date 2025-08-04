using PowerBiUpdater.Models;
using PowerBiUpdater.Services;
using PowerBiUpdater.Models;
using PowerBiUpdater.Services;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Add Serilog
builder.Services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Configuration
builder.Services.Configure<PowerBIConfig>(
    builder.Configuration.GetSection("PowerBI"));
builder.Services.Configure<ScheduleConfig>(
    builder.Configuration.GetSection("Schedule"));

// Services
builder.Services.AddHttpClient<PowerBIService>();
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IPowerBIService, PowerBIService>();
builder.Services.AddScoped<IReportUpdateService, ReportUpdateService>();

// Background service
builder.Services.AddHostedService<ReportUpdateWorker>();

// Health checks


// Configure for different hosting environments
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "PowerBI Updater Service";
});

builder.Services.AddSystemd();

var host = builder.Build();

// Use Serilog for logging
host.Services.GetRequiredService<ILoggerFactory>().AddSerilog();

await host.RunAsync();