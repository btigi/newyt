using Microsoft.EntityFrameworkCore;
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
    options.UseSqlite("Data Source=newyt.db"));

// Add HTTP client for YouTube RSS service
builder.Services.AddHttpClient<YouTubeRssService>();

// Add the background service
builder.Services.AddHostedService<VideoFetcherService>();

var host = builder.Build();

// Ensure database is created
using (var scope = host.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        context.Database.EnsureCreated();
        logger.LogInformation("Database initialized successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while creating the database");
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