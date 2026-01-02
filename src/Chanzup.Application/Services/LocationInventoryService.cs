using Microsoft.EntityFrameworkCore;
using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;

namespace Chanzup.Application.Services;

public interface ILocationInventoryService
{
    Task<bool> IsPrizeAvailableAtLocationAsync(Guid prizeId, Guid locationId, CancellationToken cancellationToken = default);
    Task<int> GetRemainingQuantityAtLocationAsync(Guid prizeId, Guid locationId, CancellationToken cancellationToken = default);
    Task SetupLocationSpecificInventoryAsync(Guid prizeId, Dictionary<Guid, int> locationQuantities, CancellationToken cancellationToken = default);
    Task ReservePrizeAtLocationAsync(Guid prizeId, Guid locationId, CancellationToken cancellationToken = default);
    Task ReleasePrizeAtLocationAsync(Guid prizeId, Guid locationId, CancellationToken cancellationToken = default);
    Task<List<PrizeLocationInventory>> GetLocationInventoryAsync(Guid locationId, CancellationToken cancellationToken = default);
    Task<List<PrizeLocationInventory>> GetPrizeInventoryAcrossLocationsAsync(Guid prizeId, CancellationToken cancellationToken = default);
    Task UpdateLocationInventoryAsync(Guid prizeId, Guid locationId, int newQuantity, CancellationToken cancellationToken = default);
    Task TransferInventoryBetweenLocationsAsync(Guid prizeId, Guid fromLocationId, Guid toLocationId, int quantity, CancellationToken cancellationToken = default);
}

public class LocationInventoryService : ILocationInventoryService
{
    private readonly IApplicationDbContext _context;

    public LocationInventoryService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsPrizeAvailableAtLocationAsync(Guid prizeId, Guid locationId, CancellationToken cancellationToken = default)
    {
        var prize = await _context.Prizes
            .Include(p => p.LocationInventories)
            .FirstOrDefaultAsync(p => p.Id == prizeId, cancellationToken);

        return prize?.IsAvailableAtLocation(locationId) ?? false;
    }

    public async Task<int> GetRemainingQuantityAtLocationAsync(Guid prizeId, Guid locationId, CancellationToken cancellationToken = default)
    {
        var prize = await _context.Prizes
            .Include(p => p.LocationInventories)
            .FirstOrDefaultAsync(p => p.Id == prizeId, cancellationToken);

        return prize?.GetRemainingQuantityAtLocation(locationId) ?? 0;
    }

    public async Task SetupLocationSpecificInventoryAsync(Guid prizeId, Dictionary<Guid, int> locationQuantities, CancellationToken cancellationToken = default)
    {
        var prize = await _context.Prizes
            .Include(p => p.LocationInventories)
            .FirstOrDefaultAsync(p => p.Id == prizeId, cancellationToken);

        if (prize == null)
            throw new ArgumentException($"Prize with ID {prizeId} not found");

        // Validate that all locations belong to the same business as the prize's campaign
        var campaignBusinessId = await _context.Campaigns
            .Where(c => c.Id == prize.CampaignId)
            .Select(c => c.BusinessId)
            .FirstOrDefaultAsync(cancellationToken);

        var validLocationIds = await _context.BusinessLocations
            .Where(bl => bl.BusinessId == campaignBusinessId && locationQuantities.Keys.Contains(bl.Id))
            .Select(bl => bl.Id)
            .ToListAsync(cancellationToken);

        var invalidLocationIds = locationQuantities.Keys.Except(validLocationIds).ToList();
        if (invalidLocationIds.Any())
            throw new ArgumentException($"Invalid location IDs: {string.Join(", ", invalidLocationIds)}");

        prize.SetupLocationSpecificInventory(locationQuantities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReservePrizeAtLocationAsync(Guid prizeId, Guid locationId, CancellationToken cancellationToken = default)
    {
        var prize = await _context.Prizes
            .Include(p => p.LocationInventories)
            .FirstOrDefaultAsync(p => p.Id == prizeId, cancellationToken);

        if (prize == null)
            throw new ArgumentException($"Prize with ID {prizeId} not found");

        prize.ReserveOneAtLocation(locationId);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReleasePrizeAtLocationAsync(Guid prizeId, Guid locationId, CancellationToken cancellationToken = default)
    {
        var prize = await _context.Prizes
            .Include(p => p.LocationInventories)
            .FirstOrDefaultAsync(p => p.Id == prizeId, cancellationToken);

        if (prize == null)
            throw new ArgumentException($"Prize with ID {prizeId} not found");

        prize.ReleaseOneAtLocation(locationId);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<PrizeLocationInventory>> GetLocationInventoryAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        return await _context.PrizeLocationInventories
            .Include(pli => pli.Prize)
            .ThenInclude(p => p.Campaign)
            .Where(pli => pli.LocationId == locationId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PrizeLocationInventory>> GetPrizeInventoryAcrossLocationsAsync(Guid prizeId, CancellationToken cancellationToken = default)
    {
        return await _context.PrizeLocationInventories
            .Include(pli => pli.Location)
            .Where(pli => pli.PrizeId == prizeId)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateLocationInventoryAsync(Guid prizeId, Guid locationId, int newQuantity, CancellationToken cancellationToken = default)
    {
        var inventory = await _context.PrizeLocationInventories
            .FirstOrDefaultAsync(pli => pli.PrizeId == prizeId && pli.LocationId == locationId, cancellationToken);

        if (inventory == null)
            throw new ArgumentException($"No inventory found for prize {prizeId} at location {locationId}");

        if (newQuantity < 0)
            throw new ArgumentException("Quantity cannot be negative");

        if (newQuantity > inventory.TotalQuantity)
            throw new ArgumentException("New quantity cannot exceed total quantity");

        inventory.RemainingQuantity = newQuantity;
        inventory.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task TransferInventoryBetweenLocationsAsync(Guid prizeId, Guid fromLocationId, Guid toLocationId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new ArgumentException("Transfer quantity must be positive");

        var fromInventory = await _context.PrizeLocationInventories
            .FirstOrDefaultAsync(pli => pli.PrizeId == prizeId && pli.LocationId == fromLocationId, cancellationToken);

        var toInventory = await _context.PrizeLocationInventories
            .FirstOrDefaultAsync(pli => pli.PrizeId == prizeId && pli.LocationId == toLocationId, cancellationToken);

        if (fromInventory == null)
            throw new ArgumentException($"No inventory found for prize {prizeId} at source location {fromLocationId}");

        if (toInventory == null)
            throw new ArgumentException($"No inventory found for prize {prizeId} at destination location {toLocationId}");

        if (fromInventory.RemainingQuantity < quantity)
            throw new ArgumentException($"Insufficient inventory at source location. Available: {fromInventory.RemainingQuantity}, Requested: {quantity}");

        // Perform the transfer
        fromInventory.RemainingQuantity -= quantity;
        fromInventory.TotalQuantity -= quantity;
        fromInventory.UpdatedAt = DateTime.UtcNow;

        toInventory.RemainingQuantity += quantity;
        toInventory.TotalQuantity += quantity;
        toInventory.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}