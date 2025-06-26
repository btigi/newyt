using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using newyt.shared.Data;
using newyt.shared.Services;
using newyt.web.Models;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Services.Configure<UIConfiguration>(
    builder.Configuration.GetSection(UIConfiguration.SectionName));

// Add services to the container.
builder.Services.AddRazorPages();

// Add Entity Framework
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=newyt.db"));

// Add HTTP client for YouTube RSS service
builder.Services.AddHttpClient<YouTubeRssService>();

// Add HTTP client for YouTube Channel Resolver
builder.Services.AddHttpClient<YouTubeChannelResolver>();

// Add HTTP client for thumbnail downloader
builder.Services.AddHttpClient<ThumbnailDownloaderService>();

// Configure thumbnail downloader for web app
builder.Services.AddSingleton<ThumbnailDownloaderService>(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    var logger = provider.GetRequiredService<ILogger<ThumbnailDownloaderService>>();
    
    // Path for web app's wwwroot/thumbnails
    var thumbnailsPath = "wwwroot/thumbnails";
    
    return new ThumbnailDownloaderService(httpClient, logger, thumbnailsPath);
});

// Add database upgrade service
builder.Services.AddScoped<DatabaseUpgradeService>();

var app = builder.Build();

// Upgrade database schema
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var databaseUpgrade = scope.ServiceProvider.GetRequiredService<DatabaseUpgradeService>();
        await databaseUpgrade.UpgradeDatabaseAsync();
        logger.LogInformation("Database schema upgrade completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database upgrade");
        // Don't exit here for web app, just log the error
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Configure static files
app.UseStaticFiles();

// Configure additional static files with explicit MIME types
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".css"] = "text/css";
provider.Mappings[".js"] = "application/javascript";
provider.Mappings[".jpg"] = "image/jpeg";
provider.Mappings[".jpeg"] = "image/jpeg";
provider.Mappings[".png"] = "image/png";
provider.Mappings[".webp"] = "image/webp";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider,
    OnPrepareResponse = ctx =>
    {
        // Set cache headers for static files
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000");
    }
});

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();