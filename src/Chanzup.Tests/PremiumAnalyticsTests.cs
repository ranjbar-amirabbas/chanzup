using Microsoft.EntityFrameworkCore;
using Chanzup.Application.Services;
using Chanzup.Application.DTOs;
using Chanzup.Domain.Entities;
using Chanzup.Infrastructure.Data;
using Chanzup.Infrastructure.Services;
using Xunit;

namespace Chanzup.Tests;

public class PremiumAnalyticsTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AnalyticsService _analyticsService;

    public PremiumAnalyticsTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantContext = new TenantContext();
        _context = new ApplicationDbContext(options, tenantContext);
        _analyticsService = new AnalyticsService(_context);
    }

    [Fact]
    public async Task GetPremiumAnalytics_WithPremiumSubscription_ReturnsAnalytics()
    {
        // Arrange
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Premium Test Business",
            Email = new Domain.ValueObjects.Email("premium@test.com"),
            SubscriptionTier = SubscriptionTier.Premium,
            IsActive = true
        };

        _context.Businesses.Add(business);
        await _context.SaveChangesAsync();

        // Act
        var result = await _analyticsService.GetPremiumAnalyticsAsync(business.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(business.Id, result.BusinessId);
        Assert.Equal(business.Name, result.BusinessName);
        Assert.Equal("Premium", result.SubscriptionTier);
        Assert.NotNull(result.Performance);
        Assert.NotNull(result.Predictions);
        Assert.NotNull(result.Benchmarks);
        Assert.NotNull(result.PlayerInsights);
        Assert.NotNull(result.Revenue);
        Assert.NotNull(result.Recommendations);
    }

    [Fact]
    public async Task GetPremiumAnalytics_WithBasicSubscription_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Basic Test Business",
            Email = new Domain.ValueObjects.Email("basic@test.com"),
            SubscriptionTier = SubscriptionTier.Basic,
            IsActive = true
        };

        _context.Businesses.Add(business);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _analyticsService.GetPremiumAnalyticsAsync(business.Id));
        
        Assert.Contains("Premium subscription required", exception.Message);
    }

    [Fact]
    public async Task GetAdvancedCampaignComparison_WithPremiumSubscription_ReturnsComparison()
    {
        // Arrange
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Premium Test Business",
            Email = new Domain.ValueObjects.Email("premium@test.com"),
            SubscriptionTier = SubscriptionTier.Premium,
            IsActive = true
        };

        var campaign1 = new Campaign
        {
            Id = Guid.NewGuid(),
            BusinessId = business.Id,
            Name = "Test Campaign 1",
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        var campaign2 = new Campaign
        {
            Id = Guid.NewGuid(),
            BusinessId = business.Id,
            Name = "Test Campaign 2",
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        _context.Businesses.Add(business);
        _context.Campaigns.AddRange(campaign1, campaign2);
        await _context.SaveChangesAsync();

        var campaignIds = new List<Guid> { campaign1.Id, campaign2.Id };

        // Act
        var result = await _analyticsService.GetAdvancedCampaignComparisonAsync(business.Id, campaignIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(campaignIds, result.CampaignIds);
        Assert.NotNull(result.StatisticalAnalysis);
        Assert.NotNull(result.PerformanceRankings);
        Assert.NotNull(result.CrossCampaignInsights);
        Assert.NotNull(result.OptimizationOpportunities);
    }

    [Fact]
    public async Task GetOptimizationRecommendations_WithPremiumSubscription_ReturnsRecommendations()
    {
        // Arrange
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Premium Test Business",
            Email = new Domain.ValueObjects.Email("premium@test.com"),
            SubscriptionTier = SubscriptionTier.Premium,
            IsActive = true
        };

        _context.Businesses.Add(business);
        await _context.SaveChangesAsync();

        // Act
        var result = await _analyticsService.GetOptimizationRecommendationsAsync(business.Id);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<OptimizationRecommendation>>(result);
        // Recommendations should be ordered by potential impact * confidence score
        if (result.Count > 1)
        {
            for (int i = 0; i < result.Count - 1; i++)
            {
                var currentScore = result[i].PotentialImpact * result[i].ConfidenceScore;
                var nextScore = result[i + 1].PotentialImpact * result[i + 1].ConfidenceScore;
                Assert.True(currentScore >= nextScore, "Recommendations should be ordered by impact * confidence");
            }
        }
    }

    [Fact]
    public async Task GetPredictiveAnalytics_WithPremiumSubscription_ReturnsPredictions()
    {
        // Arrange
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Premium Test Business",
            Email = new Domain.ValueObjects.Email("premium@test.com"),
            SubscriptionTier = SubscriptionTier.Premium,
            IsActive = true
        };

        _context.Businesses.Add(business);
        await _context.SaveChangesAsync();

        // Act
        var result = await _analyticsService.GetPredictiveAnalyticsAsync(business.Id);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.PredictedNewPlayersNextMonth >= 0);
        Assert.True(result.PredictedChurnRate >= 0);
        Assert.True(result.PredictedActivePlayersNextWeek >= 0);
        Assert.True(result.PredictedRevenueNextMonth >= 0);
        Assert.True(result.PredictedTokenSalesNextWeek >= 0);
        Assert.NotNull(result.CampaignPredictions);
        Assert.NotNull(result.SeasonalTrends);
        Assert.NotNull(result.RiskIndicators);
    }

    [Theory]
    [InlineData(SubscriptionTier.Basic)]
    [InlineData(SubscriptionTier.Suspended)]
    public async Task PremiumAnalyticsMethods_WithInsufficientSubscription_ThrowUnauthorizedAccessException(SubscriptionTier tier)
    {
        // Arrange
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Test Business",
            Email = new Domain.ValueObjects.Email("test@test.com"),
            SubscriptionTier = tier,
            IsActive = true
        };

        _context.Businesses.Add(business);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _analyticsService.GetPremiumAnalyticsAsync(business.Id));
        
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _analyticsService.GetAdvancedCampaignComparisonAsync(business.Id, new List<Guid> { Guid.NewGuid() }));
        
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _analyticsService.GetOptimizationRecommendationsAsync(business.Id));
        
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _analyticsService.GetPredictiveAnalyticsAsync(business.Id));
    }

    [Fact]
    public async Task PremiumAnalytics_WithNonExistentBusiness_ThrowsArgumentException()
    {
        // Arrange
        var nonExistentBusinessId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _analyticsService.GetPremiumAnalyticsAsync(nonExistentBusinessId));
        
        Assert.Contains("Business with ID", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}