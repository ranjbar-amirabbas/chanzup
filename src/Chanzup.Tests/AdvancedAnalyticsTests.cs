using Microsoft.EntityFrameworkCore;
using Chanzup.Application.Interfaces;
using Chanzup.Application.Services;
using Chanzup.Application.DTOs;
using Chanzup.Domain.Entities;
using Chanzup.Domain.ValueObjects;
using Chanzup.Infrastructure.Data;
using Chanzup.Infrastructure.Services;
using Xunit;

namespace Chanzup.Tests;

/// <summary>
/// Unit tests for advanced analytics functionality
/// </summary>
public class AdvancedAnalyticsTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantContext = new TenantContext();
        return new ApplicationDbContext(options, tenantContext);
    }

    private IAnalyticsService CreateAnalyticsService(ApplicationDbContext context)
    {
        return new AnalyticsService(context);
    }

    [Fact]
    public async Task GetDetailedCampaignPerformanceAsync_ShouldReturnDetailedMetrics()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var analyticsService = CreateAnalyticsService(context);

        var business = CreateTestBusiness(context);
        var player = CreateTestPlayer(context);
        var campaign = CreateTestCampaign(context, business);
        var prize = CreateTestPrize(context, campaign);

        // Create some test data
        var qrSession = new QRSession
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            BusinessId = business.Id,
            TokensEarned = 10,
            SessionHash = "test-hash",
            CreatedAt = DateTime.UtcNow
        };

        var wheelSpin = new WheelSpin
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            CampaignId = campaign.Id,
            PrizeId = prize.Id,
            TokensSpent = 5,
            SpinResult = "win",
            RandomSeed = "test-seed",
            CreatedAt = DateTime.UtcNow
        };

        context.QRSessions.Add(qrSession);
        context.WheelSpins.Add(wheelSpin);
        await context.SaveChangesAsync();

        // Act
        var result = await analyticsService.GetDetailedCampaignPerformanceAsync(campaign.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(campaign.Id, result.CampaignId);
        Assert.Equal(campaign.Name, result.CampaignName);
        Assert.Equal(1, result.Performance.TotalSpins);
        Assert.Equal(1, result.Performance.UniquePlayers);
        Assert.Equal(1, result.Performance.PrizesAwarded);
        Assert.Single(result.PrizePerformance);
        Assert.NotEmpty(result.HourlyBreakdown);
        Assert.NotEmpty(result.DailyBreakdown);
    }

    [Fact]
    public async Task GetComprehensivePlayerBehaviorAsync_ShouldReturnPlayerInsights()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var analyticsService = CreateAnalyticsService(context);

        var business = CreateTestBusiness(context);
        var player = CreateTestPlayer(context);
        var campaign = CreateTestCampaign(context, business);

        // Create some test data
        var qrSession = new QRSession
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            BusinessId = business.Id,
            TokensEarned = 10,
            SessionHash = "test-hash",
            CreatedAt = DateTime.UtcNow
        };

        context.QRSessions.Add(qrSession);
        await context.SaveChangesAsync();

        // Act
        var result = await analyticsService.GetComprehensivePlayerBehaviorAsync(business.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(business.Id, result.BusinessId);
        Assert.Equal(business.Name, result.BusinessName);
        Assert.Equal(1, result.Demographics.TotalPlayers);
        Assert.NotNull(result.EngagementPatterns);
        Assert.NotNull(result.BehavioralInsights);
        Assert.NotNull(result.Retention);
    }

    [Fact]
    public async Task GenerateExportableReportAsync_ShouldReturnReportResponse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var analyticsService = CreateAnalyticsService(context);

        var business = CreateTestBusiness(context);
        await context.SaveChangesAsync();

        var request = new ReportGenerationRequest
        {
            ReportType = "business",
            Format = "CSV",
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow,
            IncludeMetrics = new List<string> { "spins", "players", "revenue" },
            Filters = new Dictionary<string, object>(),
            IncludeCharts = false,
            IncludeRawData = true
        };

        // Act
        var result = await analyticsService.GenerateExportableReportAsync(business.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.ReportId);
        Assert.Equal("business", result.ReportType);
        Assert.Equal("CSV", result.Format);
        Assert.NotEmpty(result.DownloadUrl);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
        Assert.Equal(business.Id, result.Metadata.BusinessId);
        Assert.Equal(business.Name, result.Metadata.BusinessName);
    }

    [Fact]
    public async Task GetAllCampaignPerformancesAsync_ShouldReturnAllCampaigns()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var analyticsService = CreateAnalyticsService(context);

        var business = CreateTestBusiness(context);
        var campaign1 = CreateTestCampaign(context, business);
        var campaign2 = CreateTestCampaign(context, business);
        campaign2.Name = "Second Campaign";

        await context.SaveChangesAsync();

        // Act
        var result = await analyticsService.GetAllCampaignPerformancesAsync(business.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.CampaignId == campaign1.Id);
        Assert.Contains(result, r => r.CampaignId == campaign2.Id);
    }

    private Business CreateTestBusiness(ApplicationDbContext context)
    {
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Test Business",
            Email = new Email("test@business.com"),
            Address = "123 Test St",
            Location = new Location(49.2827m, -123.1207m),
            IsActive = true
        };

        context.Businesses.Add(business);
        return business;
    }

    private Player CreateTestPlayer(ApplicationDbContext context)
    {
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Email = new Email("test@player.com"),
            PasswordHash = "hashedpassword",
            FirstName = "Test",
            LastName = "Player",
            TokenBalance = 100,
            IsActive = true
        };

        context.Players.Add(player);
        return player;
    }

    private Campaign CreateTestCampaign(ApplicationDbContext context, Business business)
    {
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            BusinessId = business.Id,
            Name = "Test Campaign",
            Description = "Test campaign description",
            GameType = GameType.WheelOfLuck,
            TokenCostPerSpin = 5,
            MaxSpinsPerDay = 10,
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        context.Campaigns.Add(campaign);
        return campaign;
    }

    private Prize CreateTestPrize(ApplicationDbContext context, Campaign campaign)
    {
        var prize = new Prize
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Name = "Test Prize",
            Description = "Test prize description",
            Value = new Money(10.00m, "CAD"),
            TotalQuantity = 100,
            RemainingQuantity = 100,
            WinProbability = 0.1m,
            IsActive = true
        };

        context.Prizes.Add(prize);
        return prize;
    }
}