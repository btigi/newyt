using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using newyt.console.Services;
using newyt.shared.Data;
using newyt.shared.Services;

var builder = Host.CreateApplicationBuilder(args);

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Add Entity Framework
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=../../shared-data/newyt.db"));

// Add HTTP client for YouTube RSS service
builder.Services.AddHttpClient<YouTubeRssService>();

// Add HTTP client for thumbnail downloader
builder.Services.AddHttpClient<ThumbnailDownloaderService>();

// Configure thumbnail downloader with correct path
builder.Services.AddSingleton<ThumbnailDownloaderService>(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    var logger = provider.GetRequiredService<ILogger<ThumbnailDownloaderService>>();
    
    // Path relative to console app that points to web app's wwwroot/thumbnails
    var thumbnailsPath = Path.GetFullPath("../newyt.web/wwwroot/thumbnails");
    
    return new ThumbnailDownloaderService(httpClient, logger, thumbnailsPath);
});

// Add database upgrade service
builder.Services.AddScoped<DatabaseUpgradeService>();

// Add thumbnail migration service
builder.Services.AddScoped<ThumbnailMigrationService>();

// Add the background service
builder.Services.AddHostedService<VideoFetcherService>();

var host = builder.Build();

// Upgrade database schema and run thumbnail migration
using (var scope = host.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // First, upgrade the database schema
        var databaseUpgrade = scope.ServiceProvider.GetRequiredService<DatabaseUpgradeService>();
        await databaseUpgrade.UpgradeDatabaseAsync();
        
        // Then run thumbnail migration for existing videos
        var thumbnailMigration = scope.ServiceProvider.GetRequiredService<ThumbnailMigrationService>();
        await thumbnailMigration.DownloadMissingThumbnailsAsync();
        
        logger.LogInformation("Database upgrade and thumbnail migration completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database upgrade or thumbnail migration");
        Environment.Exit(1);
    }
}

Console.WriteLine("NewYT Video Fetcher Service starting...");
Console.WriteLine("Press Ctrl+C to stop the service");

try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Application terminated unexpectedly: {ex.Message}");
    Environment.Exit(1);
}