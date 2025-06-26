using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using newyt.shared.Data;

namespace newyt.shared.Services;

public class DatabaseUpgradeService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseUpgradeService> _logger;

    public DatabaseUpgradeService(AppDbContext context, ILogger<DatabaseUpgradeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task UpgradeDatabaseAsync()
    {
        _logger.LogInformation("Checking database schema for required upgrades...");

        try
        {
            // Ensure database exists first
            await _context.Database.EnsureCreatedAsync();

            // Check if ThumbnailPath column exists
            if (!await ColumnExistsAsync("Videos", "ThumbnailPath"))
            {
                _logger.LogInformation("ThumbnailPath column not found. Adding to Videos table...");
                await AddThumbnailPathColumnAsync();
                _logger.LogInformation("Successfully added ThumbnailPath column to Videos table");
            }
            else
            {
                _logger.LogInformation("ThumbnailPath column already exists. No upgrade needed.");
            }

            _logger.LogInformation("Database schema upgrade completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database schema upgrade");
            throw;
        }
    }

    private async Task<bool> ColumnExistsAsync(string tableName, string columnName)
    {
        try
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            using var command = new SqliteCommand($"PRAGMA table_info({tableName})", connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                // PRAGMA table_info returns: cid, name, type, notnull, dflt_value, pk
                // Column name is at index 1
                var colName = reader.GetString(1);
                if (string.Equals(colName, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if column {ColumnName} exists in table {TableName}", columnName, tableName);
            throw;
        }
    }

    private async Task AddThumbnailPathColumnAsync()
    {
        try
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            // Add the ThumbnailPath column with a default value of NULL
            var sql = "ALTER TABLE Videos ADD COLUMN ThumbnailPath TEXT NULL";
            using var command = new SqliteCommand(sql, connection);
            await command.ExecuteNonQueryAsync();

            _logger.LogInformation("Successfully executed: {Sql}", sql);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding ThumbnailPath column to Videos table");
            throw;
        }
    }

    public async Task<int> GetVideosWithoutThumbnailsCountAsync()
    {
        try
        {
            return await _context.Videos
                .Where(v => v.ThumbnailPath == null || v.ThumbnailPath == string.Empty)
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting videos without thumbnails");
            return 0;
        }
    }
} 