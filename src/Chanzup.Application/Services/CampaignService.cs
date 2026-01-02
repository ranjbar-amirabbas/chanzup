using Microsoft.EntityFrameworkCore;
using Chanzup.Application.DTOs;
using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;
using Chanzup.Domain.ValueObjects;

namespace Chanzup.Application.Services;

public class CampaignService : ICampaignService
{
    private readonly IApplicationDbContext _context;
    private readonly IQRCodeService _qrCodeService;

    public CampaignService(IApplicationDbContext context, IQRCodeService qrCodeService)
    {
        _context = context;
        _qrCodeService = qrCodeService;
    }

    public async Task<CampaignResponse> CreateCampaignAsync(Guid businessId, CreateCampaignRequest request, CancellationToken cancellationToken = default)
    {
        // Validate that the business exists
        var business = await _context.Businesses
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
        
        if (business == null)
            throw new ArgumentException("Business not found", nameof(businessId));

        // Validate prize probabilities sum to <= 1.0
        var totalProbability = request.Prizes.Sum(p => p.WinProbability);
        if (totalProbability > 1.0m)
            throw new ArgumentException("Total prize probabilities cannot exceed 1.0");

        // Create campaign
        var campaign = new Campaign
        {
            BusinessId = businessId,
            Name = request.Name,
            Description = request.Description,
            GameType = request.GameType,
            TokenCostPerSpin = request.TokenCostPerSpin,
            MaxSpinsPerDay = request.MaxSpinsPerDay,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = true
        };

        // Create prizes
        foreach (var prizeRequest in request.Prizes)
        {
            var prize = new Prize
            {
                CampaignId = campaign.Id,
                Name = prizeRequest.Name,
                Description = prizeRequest.Description,
                Value = prizeRequest.Value.HasValue ? new Money(prizeRequest.Value.Value, "CAD") : null,
                TotalQuantity = prizeRequest.TotalQuantity,
                RemainingQuantity = prizeRequest.TotalQuantity,
                WinProbability = prizeRequest.WinProbability,
                IsActive = true
            };
            campaign.Prizes.Add(prize);
        }

        _context.Campaigns.Add(campaign);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToCampaignResponse(campaign);
    }

    public async Task<CampaignResponse?> GetCampaignAsync(Guid campaignId, Guid businessId, CancellationToken cancellationToken = default)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.Prizes)
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.BusinessId == businessId, cancellationToken);

        return campaign == null ? null : MapToCampaignResponse(campaign);
    }

    public async Task<IEnumerable<CampaignResponse>> GetCampaignsAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var campaigns = await _context.Campaigns
            .Include(c => c.Prizes)
            .Where(c => c.BusinessId == businessId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return campaigns.Select(c => MapToCampaignResponse(c));
    }

    public async Task<CampaignResponse?> UpdateCampaignAsync(Guid campaignId, Guid businessId, UpdateCampaignRequest request, CancellationToken cancellationToken = default)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.Prizes)
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.BusinessId == businessId, cancellationToken);

        if (campaign == null)
            return null;

        // Update only provided fields
        if (!string.IsNullOrEmpty(request.Name))
            campaign.Name = request.Name;
        
        if (request.Description != null)
            campaign.Description = request.Description;
        
        if (request.TokenCostPerSpin.HasValue)
            campaign.TokenCostPerSpin = request.TokenCostPerSpin.Value;
        
        if (request.MaxSpinsPerDay.HasValue)
            campaign.MaxSpinsPerDay = request.MaxSpinsPerDay.Value;
        
        if (request.StartDate.HasValue)
            campaign.StartDate = request.StartDate.Value;
        
        if (request.EndDate.HasValue)
            campaign.EndDate = request.EndDate.Value;
        
        if (request.IsActive.HasValue)
            campaign.IsActive = request.IsActive.Value;

        campaign.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToCampaignResponse(campaign);
    }

    public async Task<bool> DeleteCampaignAsync(Guid campaignId, Guid businessId, CancellationToken cancellationToken = default)
    {
        var campaign = await _context.Campaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.BusinessId == businessId, cancellationToken);

        if (campaign == null)
            return false;

        _context.Campaigns.Remove(campaign);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ActivateCampaignAsync(Guid campaignId, Guid businessId, CancellationToken cancellationToken = default)
    {
        var campaign = await _context.Campaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.BusinessId == businessId, cancellationToken);

        if (campaign == null)
            return false;

        campaign.Activate();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeactivateCampaignAsync(Guid campaignId, Guid businessId, CancellationToken cancellationToken = default)
    {
        var campaign = await _context.Campaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.BusinessId == businessId, cancellationToken);

        if (campaign == null)
            return false;

        campaign.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<CampaignResponse>> GetActiveCampaignsNearLocationAsync(double latitude, double longitude, double radiusKm = 5.0, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var campaigns = await _context.Campaigns
            .Include(c => c.Prizes)
            .Include(c => c.Business)
            .Where(c => c.IsActive && 
                       c.StartDate <= now && 
                       (c.EndDate == null || c.EndDate >= now))
            .ToListAsync(cancellationToken);

        var searchLocation = new Domain.ValueObjects.Location((decimal)latitude, (decimal)longitude);
        var radiusMeters = radiusKm * 1000;

        // Filter by distance using Location value object
        var nearbyCampaigns = campaigns.Where(c => 
        {
            if (c.Business.Location == null)
                return false;

            return c.Business.Location.IsWithinRadius(searchLocation, radiusMeters);
        });

        return nearbyCampaigns.Select(c => MapToCampaignResponse(c, searchLocation));
    }

    private static CampaignResponse MapToCampaignResponse(Campaign campaign, Domain.ValueObjects.Location? searchLocation = null)
    {
        var response = new CampaignResponse
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            GameType = campaign.GameType,
            TokenCostPerSpin = campaign.TokenCostPerSpin,
            MaxSpinsPerDay = campaign.MaxSpinsPerDay,
            IsActive = campaign.IsActive,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            CreatedAt = campaign.CreatedAt,
            UpdatedAt = campaign.UpdatedAt,
            Prizes = campaign.Prizes.Select(p => new PrizeResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Value = p.Value?.Amount,
                TotalQuantity = p.TotalQuantity,
                RemainingQuantity = p.RemainingQuantity,
                WinProbability = p.WinProbability,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList(),
            QRCodeUrl = $"/api/qr/{campaign.Id}"
        };

        // Include business information if available
        if (campaign.Business != null)
        {
            response.Business = new BusinessInfo
            {
                Id = campaign.Business.Id,
                Name = campaign.Business.Name,
                Address = campaign.Business.Address,
                Latitude = campaign.Business.Location?.Latitude,
                Longitude = campaign.Business.Location?.Longitude
            };

            // Calculate distance if search location is provided
            if (searchLocation != null && campaign.Business.Location != null)
            {
                var distanceMeters = campaign.Business.Location.DistanceTo(searchLocation);
                response.Business.DistanceKm = Math.Round(distanceMeters / 1000.0, 2);
            }
        }

        return response;
    }
}