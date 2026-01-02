using Microsoft.EntityFrameworkCore;
using Chanzup.Domain.Entities;
using Chanzup.Domain.ValueObjects;
using BCrypt.Net;

namespace Chanzup.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Seed reference data if not exists
        await SeedSubscriptionTiers(context);
        await SeedGameTypes(context);
        await SeedTransactionTypes(context);
        
        await context.SaveChangesAsync();
    }

    private static async Task SeedSubscriptionTiers(ApplicationDbContext context)
    {
        // This is handled by the enum, no seeding needed for now
        // Could add subscription tier configuration data here if needed
        await Task.CompletedTask;
    }

    private static async Task SeedGameTypes(ApplicationDbContext context)
    {
        // This is handled by the enum, no seeding needed for now
        // Could add game type configuration data here if needed
        await Task.CompletedTask;
    }

    private static async Task SeedTransactionTypes(ApplicationDbContext context)
    {
        // This is handled by the enum, no seeding needed for now
        // Could add transaction type configuration data here if needed
        await Task.CompletedTask;
    }

    public static async Task SeedDemoDataAsync(ApplicationDbContext context)
    {
        // Only seed demo data if no businesses exist
        if (await context.Businesses.AnyAsync())
            return;

        // Create demo business
        var demoBusiness = new Business
        {
            Name = "Demo Coffee Shop",
            Email = new Email("demo@coffeeshop.com"),
            Phone = "+1-604-555-0123",
            Address = "123 Main St, Vancouver, BC",
            SubscriptionTier = SubscriptionTier.Premium,
            IsActive = true
        };

        demoBusiness.UpdateLocation(49.2827m, -123.1207m);
        context.Businesses.Add(demoBusiness);
        await context.SaveChangesAsync();

        // Create demo business location
        var demoLocation = new BusinessLocation
        {
            BusinessId = demoBusiness.Id,
            Name = "Main Location",
            Address = "123 Main St, Vancouver, BC",
            QRCode = "DEMO-QR-CODE-123",
            IsActive = true
        };
        demoLocation.UpdateLocation(49.2827m, -123.1207m);
        context.BusinessLocations.Add(demoLocation);

        // Create demo staff
        var demoStaff = new Staff
        {
            BusinessId = demoBusiness.Id,
            Email = new Email("owner@coffeeshop.com"),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("DemoPassword123!"),
            FirstName = "Demo",
            LastName = "Owner",
            Role = StaffRole.Owner,
            IsActive = true
        };
        context.Staff.Add(demoStaff);

        // Create demo campaign
        var demoCampaign = new Campaign
        {
            BusinessId = demoBusiness.Id,
            Name = "Holiday Wheel of Fortune",
            Description = "Spin to win holiday prizes!",
            GameType = GameType.WheelOfLuck,
            TokenCostPerSpin = 5,
            MaxSpinsPerDay = 3,
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        context.Campaigns.Add(demoCampaign);
        await context.SaveChangesAsync();

        // Create demo prizes
        var demoPrizes = new[]
        {
            new Prize
            {
                CampaignId = demoCampaign.Id,
                Name = "Free Coffee",
                Description = "One free regular coffee",
                Value = new Money(5.00m, "CAD"),
                TotalQuantity = 100,
                RemainingQuantity = 100,
                WinProbability = 0.3m,
                IsActive = true
            },
            new Prize
            {
                CampaignId = demoCampaign.Id,
                Name = "10% Discount",
                Description = "10% off your next purchase",
                Value = new Money(0m, "CAD"),
                TotalQuantity = 200,
                RemainingQuantity = 200,
                WinProbability = 0.5m,
                IsActive = true
            },
            new Prize
            {
                CampaignId = demoCampaign.Id,
                Name = "Free Pastry",
                Description = "One free pastry of your choice",
                Value = new Money(3.50m, "CAD"),
                TotalQuantity = 50,
                RemainingQuantity = 50,
                WinProbability = 0.15m,
                IsActive = true
            }
        };

        context.Prizes.AddRange(demoPrizes);

        // Create demo player
        var demoPlayer = new Player
        {
            Email = new Email("demo@player.com"),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("PlayerPassword123!"),
            FirstName = "Demo",
            LastName = "Player",
            Phone = "+1-604-555-0456",
            TokenBalance = 25,
            IsActive = true
        };
        context.Players.Add(demoPlayer);

        await context.SaveChangesAsync();
    }
}