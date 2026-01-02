using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Chanzup.Infrastructure.Data;

namespace Chanzup.Infrastructure.Extensions;

public static class DatabaseExtensions
{
    public static async Task<IHost> MigrateDatabaseAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            
            logger.LogInformation("Starting database initialization...");
            
            // For development, use EnsureCreated instead of migrations
            var environment = services.GetRequiredService<IHostEnvironment>();
            if (environment.IsDevelopment() || environment.EnvironmentName == "Local")
            {
                await context.Database.EnsureCreatedAsync();
                logger.LogInformation("Database schema created successfully.");
            }
            else
            {
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migration completed successfully.");
            }

            logger.LogInformation("Starting database seeding...");
            await DatabaseSeeder.SeedAsync(context);
            logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }

        return host;
    }

    public static async Task<IHost> SeedDemoDataAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            
            logger.LogInformation("Starting demo data seeding...");
            await DatabaseSeeder.SeedDemoDataAsync(context);
            logger.LogInformation("Demo data seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding demo data.");
            throw;
        }

        return host;
    }
}