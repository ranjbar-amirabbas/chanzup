using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Chanzup.API.Controllers;
using Chanzup.Application.Interfaces;
using Chanzup.Application.DTOs;
using Chanzup.Domain.Entities;
using Chanzup.Domain.ValueObjects;
using Xunit;

namespace Chanzup.Tests;

/// <summary>
/// Unit tests for advanced analytics controller endpoints
/// </summary>
public class AdvancedAnalyticsControllerTests
{
    private readonly Mock<IAnalyticsService> _mockAnalyticsService;
    private readonly AnalyticsController _controller;

    public AdvancedAnalyticsControllerTests()
    {
        _mockAnalyticsService = new Mock<IAnalyticsService>();
        _controller = new AnalyticsController(_mockAnalyticsService.Object);
    }

    [Fact]
    public async Task GetDetailedCampaignPerformance_ShouldReturnOkResult()
    {
        // Arrange
        var campaignId = Guid.NewGuid();
        var expectedResponse = new DetailedCampaignPerformanceResponse
        {
            CampaignId = campaignId,
            CampaignName = "Test Campaign",
            Performance = new CampaignPerformanceMetrics
            {
                TotalSpins = 100,
                UniquePlayers = 50
            }
        };

        _mockAnalyticsService
            .Setup(s => s.GetDetailedCampaignPerformanceAsync(campaignId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetDetailedCampaignPerformance(campaignId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DetailedCampaignPerformanceResponse>(okResult.Value);
        Assert.Equal(campaignId, response.CampaignId);
        Assert.Equal("Test Campaign", response.CampaignName);
        Assert.Equal(100, response.Performance.TotalSpins);
    }

    [Fact]
    public async Task GetComprehensivePlayerBehavior_ShouldReturnOkResult()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var expectedResponse = new ComprehensivePlayerBehaviorResponse
        {
            BusinessId = businessId,
            BusinessName = "Test Business",
            Demographics = new PlayerDemographicsMetrics
            {
                TotalPlayers = 200,
                ActivePlayersToday = 50
            }
        };

        _mockAnalyticsService
            .Setup(s => s.GetComprehensivePlayerBehaviorAsync(businessId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetComprehensivePlayerBehavior(businessId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ComprehensivePlayerBehaviorResponse>(okResult.Value);
        Assert.Equal(businessId, response.BusinessId);
        Assert.Equal("Test Business", response.BusinessName);
        Assert.Equal(200, response.Demographics.TotalPlayers);
    }

    [Fact]
    public async Task GenerateExportableReport_WithValidRequest_ShouldReturnOkResult()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var request = new ReportGenerationRequest
        {
            ReportType = "business",
            Format = "CSV",
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow,
            IncludeMetrics = new List<string> { "spins", "players" }
        };

        var expectedResponse = new ExportableReportResponse
        {
            ReportId = "test-report-123",
            ReportType = "business",
            Format = "CSV",
            DownloadUrl = "https://api.test.com/reports/test-report-123/download"
        };

        _mockAnalyticsService
            .Setup(s => s.GenerateExportableReportAsync(businessId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GenerateExportableReport(businessId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ExportableReportResponse>(okResult.Value);
        Assert.Equal("test-report-123", response.ReportId);
        Assert.Equal("business", response.ReportType);
        Assert.Equal("CSV", response.Format);
    }

    [Fact]
    public async Task GenerateExportableReport_WithInvalidReportType_ShouldReturnBadRequest()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var request = new ReportGenerationRequest
        {
            ReportType = "invalid-type",
            Format = "CSV",
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _controller.GenerateExportableReport(businessId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("ReportType must be one of", badRequestResult.Value?.ToString());
    }

    [Fact]
    public async Task GetAllCampaignPerformances_ShouldReturnOkResult()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var expectedResponse = new List<DetailedCampaignPerformanceResponse>
        {
            new DetailedCampaignPerformanceResponse
            {
                CampaignId = Guid.NewGuid(),
                CampaignName = "Campaign 1"
            },
            new DetailedCampaignPerformanceResponse
            {
                CampaignId = Guid.NewGuid(),
                CampaignName = "Campaign 2"
            }
        };

        _mockAnalyticsService
            .Setup(s => s.GetAllCampaignPerformancesAsync(businessId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetAllCampaignPerformances(businessId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<List<DetailedCampaignPerformanceResponse>>(okResult.Value);
        Assert.Equal(2, response.Count);
        Assert.Equal("Campaign 1", response[0].CampaignName);
        Assert.Equal("Campaign 2", response[1].CampaignName);
    }

    [Fact]
    public async Task CompareCampaignPerformances_WithValidCampaignIds_ShouldReturnOkResult()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var campaignId1 = Guid.NewGuid();
        var campaignId2 = Guid.NewGuid();
        var campaignIds = new List<Guid> { campaignId1, campaignId2 };

        var expectedResponse1 = new DetailedCampaignPerformanceResponse
        {
            CampaignId = campaignId1,
            CampaignName = "Campaign 1"
        };

        var expectedResponse2 = new DetailedCampaignPerformanceResponse
        {
            CampaignId = campaignId2,
            CampaignName = "Campaign 2"
        };

        _mockAnalyticsService
            .Setup(s => s.GetDetailedCampaignPerformanceAsync(campaignId1, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse1);

        _mockAnalyticsService
            .Setup(s => s.GetDetailedCampaignPerformanceAsync(campaignId2, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse2);

        // Act
        var result = await _controller.CompareCampaignPerformances(businessId, campaignIds);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<List<DetailedCampaignPerformanceResponse>>(okResult.Value);
        Assert.Equal(2, response.Count);
    }

    [Fact]
    public async Task CompareCampaignPerformances_WithTooManyCampaigns_ShouldReturnBadRequest()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var campaignIds = Enumerable.Range(0, 11).Select(_ => Guid.NewGuid()).ToList();

        // Act
        var result = await _controller.CompareCampaignPerformances(businessId, campaignIds);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Maximum 10 campaigns", badRequestResult.Value?.ToString());
    }

    [Fact]
    public async Task GetAnalyticsDashboard_ShouldReturnOkResult()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var businessMetrics = new BusinessMetrics
        {
            BusinessId = businessId,
            BusinessName = "Test Business",
            TotalTokenRevenue = 1000,
            UniquePlayers = 100
        };

        var playerEngagement = new PlayerEngagementMetrics
        {
            BusinessId = businessId,
            TotalPlayers = 100,
            ActivePlayersToday = 25
        };

        var campaignPerformances = new List<DetailedCampaignPerformanceResponse>
        {
            new DetailedCampaignPerformanceResponse
            {
                CampaignId = Guid.NewGuid(),
                CampaignName = "Top Campaign",
                Performance = new CampaignPerformanceMetrics { TotalSpins = 500 }
            }
        };

        var dailyMetrics = new List<DailyMetrics>
        {
            new DailyMetrics { Date = DateTime.UtcNow.Date, Spins = 50 }
        };

        _mockAnalyticsService
            .Setup(s => s.GetBusinessMetricsAsync(businessId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(businessMetrics);

        _mockAnalyticsService
            .Setup(s => s.GetPlayerEngagementMetricsAsync(businessId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(playerEngagement);

        _mockAnalyticsService
            .Setup(s => s.GetAllCampaignPerformancesAsync(businessId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaignPerformances);

        _mockAnalyticsService
            .Setup(s => s.GetDailyMetricsAsync(businessId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dailyMetrics);

        // Act
        var result = await _controller.GetAnalyticsDashboard(businessId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }
}