using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using newyt.shared.Data;
using newyt.shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add Entity Framework
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=newyt.db"));

// Add HTTP client for YouTube RSS service
builder.Services.AddHttpClient<YouTubeRssService>();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Configure static files with explicit MIME types
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".css"] = "text/css";
provider.Mappings[".js"] = "application/javascript";

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