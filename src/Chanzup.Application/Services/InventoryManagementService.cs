using Microsoft.EntityFrameworkCore;
using Chanzup.Application.Interfaces;
using Chanzup.Application.DTOs;
using Chanzup.Domain.Entities;

namespace Chanzup.Application.Services;

public interface IInventoryManagementService
{
    Task<LocationInventoryResponse> GetLocationInventoryAsync(Guid locationId, CancellationToken cancellationToken = default);
    Task<PrizeLocationInventoryResponse> GetPrizeInventoryAcrossLocationsAsync(Guid prizeId, CancellationToken cancellationToken = default);
    Task<List<LocationInventoryResponse>> GetBusinessInventoryOverviewAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task<InventoryAlertResponse> GetInventoryAlertsAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task<InventoryTransferResponse> TransferInventoryAsync(TransferInventoryRequest request, CancellationToken cancellationToken = default);
    Task SetupLocationInventoryAsync(SetupLocationInventoryRequest request, CancellationToken cancellationToken = default);
    Task UpdateLocationInventoryAsync(UpdateLocationInventoryRequest request, CancellationToken cancellationToken = default);
}

public class InventoryManagementService : IInventoryManagementService
{
    private readonly IApplicationDbContext _context;
    private readonly ILocationInventoryService _locationInventoryService;

    public InventoryManagementService(IApplicationDbContext context, ILocationInventoryService locationInventoryService)
    {
        _context = context;
        _locationInventoryService = locationInventoryService;
    }

    public async Task<LocationInventoryResponse> GetLocationInventoryAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        var location = await _context.BusinessLocations
            .FirstOrDefaultAsync(bl => bl.Id == locationId, cancellationToken);

        if (location == null)
            throw new ArgumentException($"Location with ID {locationId} not found");

        var inventories = await _locationInventoryService.GetLocationInventoryAsync(locationId, cancellationToken);

        var prizeInventories = inventories.Select(inv => new PrizeInventoryItem
        {
            PrizeId = inv.PrizeId,
            PrizeName = inv.Prize.Name,
            CampaignName = inv.Prize.Campaign.Name,
            TotalQuantity = inv.TotalQuantity,
            RemainingQuantity = inv.RemainingQuantity,
            UtilizationRate = inv.TotalQuantity > 0 ? (decimal)(inv.TotalQuantity - inv.RemainingQuantity) / inv.TotalQuantity * 100 : 0,
            IsLowStock = inv.TotalQuantity > 0 && (decimal)inv.RemainingQuantity / inv.TotalQuantity < 0.2m,
            IsOutOfStock = inv.RemainingQuantity == 0,
            LastUpdated = inv.UpdatedAt
        }).ToList();

        var totalQuantity = inventories.Sum(inv => inv.TotalQuantity);
        var totalRemaining = inventories.Sum(inv => inv.RemainingQuantity);

        return new LocationInventoryResponse
        {
            LocationId = locationId,
            LocationName = location.Name,
            Address = location.Address,
            PrizeInventories = prizeInventories,
            TotalPrizes = inventories.Count,
            TotalRemainingQuantity = totalRemaining,
            InventoryUtilization = totalQuantity > 0 ? (decimal)(totalQuantity - totalRemaining) / totalQuantity * 100 : 0
        };
    }

    public async Task<PrizeLocationInventoryResponse> GetPrizeInventoryAcrossLocationsAsync(Guid prizeId, CancellationToken cancellationToken = default)
    {
        var prize = await _context.Prizes
            .Include(p => p.Campaign)
            .Include(p => p.LocationInventories)
            .ThenInclude(li => li.Location)
            .FirstOrDefaultAsync(p => p.Id == prizeId, cancellationToken);

        if (prize == null)
            throw new ArgumentException($"Prize with ID {prizeId} not found");

        var locationInventories = prize.LocationInventories.Select(inv => new LocationInventoryItem
        {
            LocationId = inv.LocationId,
            LocationName = inv.Location.Name,
            Address = inv.Location.Address,
            TotalQuantity = inv.TotalQuantity,
            RemainingQuantity = inv.RemainingQuantity,
            UtilizationRate = inv.TotalQuantity > 0 ? (decimal)(inv.TotalQuantity - inv.RemainingQuantity) / inv.TotalQuantity * 100 : 0,
            IsLowStock = inv.TotalQuantity > 0 && (decimal)inv.RemainingQuantity / inv.TotalQuantity < 0.2m,
            IsOutOfStock = inv.RemainingQuantity == 0,
            LastUpdated = inv.UpdatedAt
        }).ToList();

        return new PrizeLocationInventoryResponse
        {
            PrizeId = prizeId,
            PrizeName = prize.Name,
            CampaignName = prize.Campaign.Name,
            HasLocationSpecificInventory = prize.HasLocationSpecificInventory,
            TotalQuantityAcrossLocations = prize.HasLocationSpecificInventory ? prize.LocationInventories.Sum(li => li.TotalQuantity) : prize.TotalQuantity,
            RemainingQuantityAcrossLocations = prize.HasLocationSpecificInventory ? prize.LocationInventories.Sum(li => li.RemainingQuantity) : prize.RemainingQuantity,
            LocationInventories = locationInventories
        };
    }

    public async Task<List<LocationInventoryResponse>> GetBusinessInventoryOverviewAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var locations = await _context.BusinessLocations
            .Where(bl => bl.BusinessId == businessId && bl.IsActive)
            .ToListAsync(cancellationToken);

        var inventoryResponses = new List<LocationInventoryResponse>();

        foreach (var location in locations)
        {
            try
            {
                var inventory = await GetLocationInventoryAsync(location.Id, cancellationToken);
                inventoryResponses.Add(inventory);
            }
            catch (ArgumentException)
            {
                // Skip locations with no inventory
                continue;
            }
        }

        return inventoryResponses.OrderByDescending(ir => ir.TotalRemainingQuantity).ToList();
    }

    public async Task<InventoryAlertResponse> GetInventoryAlertsAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var inventories = await _context.PrizeLocationInventories
            .Include(pli => pli.Prize)
            .Include(pli => pli.Location)
            .Where(pli => pli.Location.BusinessId == businessId)
            .ToListAsync(cancellationToken);

        var lowStockAlerts = new List<LowStockAlert>();
        var outOfStockAlerts = new List<OutOfStockAlert>();
        var overstockAlerts = new List<OverstockAlert>();

        foreach (var inventory in inventories)
        {
            var utilizationRate = inventory.TotalQuantity > 0 ? (decimal)(inventory.TotalQuantity - inventory.RemainingQuantity) / inventory.TotalQuantity : 0;

            // Out of stock
            if (inventory.RemainingQuantity == 0)
            {
                outOfStockAlerts.Add(new OutOfStockAlert
                {
                    PrizeId = inventory.PrizeId,
                    PrizeName = inventory.Prize.Name,
                    LocationId = inventory.LocationId,
                    LocationName = inventory.Location.Name,
                    OutOfStockSince = inventory.UpdatedAt, // Approximation
                    TotalQuantity = inventory.TotalQuantity
                });
            }
            // Low stock (less than 20% remaining)
            else if (inventory.TotalQuantity > 0 && (decimal)inventory.RemainingQuantity / inventory.TotalQuantity < 0.2m)
            {
                var severity = (decimal)inventory.RemainingQuantity / inventory.TotalQuantity < 0.1m ? "Critical" : "Low";
                
                lowStockAlerts.Add(new LowStockAlert
                {
                    PrizeId = inventory.PrizeId,
                    PrizeName = inventory.Prize.Name,
                    LocationId = inventory.LocationId,
                    LocationName = inventory.Location.Name,
                    RemainingQuantity = inventory.RemainingQuantity,
                    TotalQuantity = inventory.TotalQuantity,
                    UtilizationRate = utilizationRate * 100,
                    Severity = severity
                });
            }
            // Overstock (more than 90% remaining after 30 days)
            else if (inventory.TotalQuantity > 0 && 
                     (decimal)inventory.RemainingQuantity / inventory.TotalQuantity > 0.9m && 
                     inventory.CreatedAt < DateTime.UtcNow.AddDays(-30))
            {
                overstockAlerts.Add(new OverstockAlert
                {
                    PrizeId = inventory.PrizeId,
                    PrizeName = inventory.Prize.Name,
                    LocationId = inventory.LocationId,
                    LocationName = inventory.Location.Name,
                    RemainingQuantity = inventory.RemainingQuantity,
                    TotalQuantity = inventory.TotalQuantity,
                    UtilizationRate = utilizationRate * 100,
                    Recommendation = "Consider reducing inventory or transferring to higher-demand locations"
                });
            }
        }

        return new InventoryAlertResponse
        {
            LowStockAlerts = lowStockAlerts.OrderBy(alert => alert.Severity == "Critical" ? 0 : 1).ThenBy(alert => alert.UtilizationRate).ToList(),
            OutOfStockAlerts = outOfStockAlerts.OrderBy(alert => alert.OutOfStockSince).ToList(),
            OverstockAlerts = overstockAlerts.OrderByDescending(alert => alert.UtilizationRate).ToList()
        };
    }

    public async Task<InventoryTransferResponse> TransferInventoryAsync(TransferInventoryRequest request, CancellationToken cancellationToken = default)
    {
        // Get current inventory states before transfer
        var fromInventoryBefore = await _context.PrizeLocationInventories
            .Include(pli => pli.Location)
            .FirstOrDefaultAsync(pli => pli.PrizeId == request.PrizeId && pli.LocationId == request.FromLocationId, cancellationToken);

        var toInventoryBefore = await _context.PrizeLocationInventories
            .Include(pli => pli.Location)
            .FirstOrDefaultAsync(pli => pli.PrizeId == request.PrizeId && pli.LocationId == request.ToLocationId, cancellationToken);

        var prize = await _context.Prizes
            .FirstOrDefaultAsync(p => p.Id == request.PrizeId, cancellationToken);

        if (fromInventoryBefore == null || toInventoryBefore == null || prize == null)
            throw new ArgumentException("Invalid transfer request - missing inventory or prize");

        // Perform the transfer
        await _locationInventoryService.TransferInventoryBetweenLocationsAsync(
            request.PrizeId, 
            request.FromLocationId, 
            request.ToLocationId, 
            request.Quantity, 
            cancellationToken);

        // Get updated inventory states
        var fromInventoryAfter = await _context.PrizeLocationInventories
            .Include(pli => pli.Location)
            .FirstOrDefaultAsync(pli => pli.PrizeId == request.PrizeId && pli.LocationId == request.FromLocationId, cancellationToken);

        var toInventoryAfter = await _context.PrizeLocationInventories
            .Include(pli => pli.Location)
            .FirstOrDefaultAsync(pli => pli.PrizeId == request.PrizeId && pli.LocationId == request.ToLocationId, cancellationToken);

        return new InventoryTransferResponse
        {
            TransferId = Guid.NewGuid(),
            PrizeId = request.PrizeId,
            PrizeName = prize.Name,
            FromLocationId = request.FromLocationId,
            FromLocationName = fromInventoryBefore.Location.Name,
            ToLocationId = request.ToLocationId,
            ToLocationName = toInventoryBefore.Location.Name,
            QuantityTransferred = request.Quantity,
            Reason = request.Reason,
            TransferredAt = DateTime.UtcNow,
            FromLocationAfterTransfer = new LocationInventoryItem
            {
                LocationId = fromInventoryAfter!.LocationId,
                LocationName = fromInventoryAfter.Location.Name,
                Address = fromInventoryAfter.Location.Address,
                TotalQuantity = fromInventoryAfter.TotalQuantity,
                RemainingQuantity = fromInventoryAfter.RemainingQuantity,
                UtilizationRate = fromInventoryAfter.TotalQuantity > 0 ? (decimal)(fromInventoryAfter.TotalQuantity - fromInventoryAfter.RemainingQuantity) / fromInventoryAfter.TotalQuantity * 100 : 0,
                IsLowStock = fromInventoryAfter.TotalQuantity > 0 && (decimal)fromInventoryAfter.RemainingQuantity / fromInventoryAfter.TotalQuantity < 0.2m,
                IsOutOfStock = fromInventoryAfter.RemainingQuantity == 0,
                LastUpdated = fromInventoryAfter.UpdatedAt
            },
            ToLocationAfterTransfer = new LocationInventoryItem
            {
                LocationId = toInventoryAfter!.LocationId,
                LocationName = toInventoryAfter.Location.Name,
                Address = toInventoryAfter.Location.Address,
                TotalQuantity = toInventoryAfter.TotalQuantity,
                RemainingQuantity = toInventoryAfter.RemainingQuantity,
                UtilizationRate = toInventoryAfter.TotalQuantity > 0 ? (decimal)(toInventoryAfter.TotalQuantity - toInventoryAfter.RemainingQuantity) / toInventoryAfter.TotalQuantity * 100 : 0,
                IsLowStock = toInventoryAfter.TotalQuantity > 0 && (decimal)toInventoryAfter.RemainingQuantity / toInventoryAfter.TotalQuantity < 0.2m,
                IsOutOfStock = toInventoryAfter.RemainingQuantity == 0,
                LastUpdated = toInventoryAfter.UpdatedAt
            }
        };
    }

    public async Task SetupLocationInventoryAsync(SetupLocationInventoryRequest request, CancellationToken cancellationToken = default)
    {
        await _locationInventoryService.SetupLocationSpecificInventoryAsync(request.PrizeId, request.LocationQuantities, cancellationToken);
    }

    public async Task UpdateLocationInventoryAsync(UpdateLocationInventoryRequest request, CancellationToken cancellationToken = default)
    {
        await _locationInventoryService.UpdateLocationInventoryAsync(request.PrizeId, request.LocationId, request.NewQuantity, cancellationToken);
    }
}