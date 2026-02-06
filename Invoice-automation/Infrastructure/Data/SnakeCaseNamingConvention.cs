using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace InvoiceAutomation.Web.Infrastructure.Data;

/// <summary>
/// Extension methods for applying snake_case naming convention to EF Core models
/// </summary>
public static class SnakeCaseNamingConvention
{
    /// <summary>
    /// Converts a string to snake_case
    /// </summary>
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var startUnderscores = Regex.Match(input, @"^_+");
        return startUnderscores + Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
    }

    /// <summary>
    /// Applies snake_case naming convention to all entities in the model
    /// </summary>
    public static void ApplySnakeCaseNamingConvention(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Skip if table name is already set explicitly
            var tableName = entity.GetTableName();
            if (tableName != null && !tableName.Contains("AspNet"))
            {
                // Table names are already set in configurations, skip
            }

            // Convert column names to snake_case
            foreach (var property in entity.GetProperties())
            {
                var columnName = property.GetColumnName();
                if (columnName != null)
                {
                    // Only convert if not already explicitly set to snake_case
                    if (!columnName.Contains('_'))
                    {
                        property.SetColumnName(columnName.ToSnakeCase());
                    }
                }
            }

            // Convert foreign key names to snake_case
            foreach (var key in entity.GetKeys())
            {
                var keyName = key.GetName();
                if (keyName != null)
                {
                    key.SetName(keyName.ToSnakeCase());
                }
            }

            // Convert index names to snake_case
            foreach (var index in entity.GetIndexes())
            {
                var indexName = index.GetDatabaseName();
                if (indexName != null)
                {
                    index.SetDatabaseName(indexName.ToSnakeCase());
                }
            }
        }
    }
}
