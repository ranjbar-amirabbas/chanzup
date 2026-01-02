using Chanzup.Domain.ValueObjects;

namespace Chanzup.Domain.Entities;

public class Prize
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Money? Value { get; set; }
    public int TotalQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public decimal WinProbability { get; set; }
    public bool IsActive { get; set; } = true;
    public bool HasLocationSpecificInventory { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Campaign Campaign { get; set; } = null!;
    public ICollection<WheelSpin> WheelSpins { get; set; } = new List<WheelSpin>();
    public ICollection<PlayerPrize> PlayerPrizes { get; set; } = new List<PlayerPrize>();
    public ICollection<PrizeLocationInventory> LocationInventories { get; set; } = new List<PrizeLocationInventory>();

    // Domain methods
    public bool IsAvailable()
    {
        return IsActive && RemainingQuantity > 0;
    }

    public bool IsAvailableAtLocation(Guid locationId)
    {
        if (!IsActive) return false;

        if (!HasLocationSpecificInventory)
            return RemainingQuantity > 0;

        var locationInventory = LocationInventories.FirstOrDefault(li => li.LocationId == locationId);
        return locationInventory?.RemainingQuantity > 0;
    }

    public void ReserveOne()
    {
        if (!IsAvailable())
            throw new InvalidOperationException("Prize is not available");

        RemainingQuantity--;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReserveOneAtLocation(Guid locationId)
    {
        if (!IsAvailableAtLocation(locationId))
            throw new InvalidOperationException("Prize is not available at this location");

        if (HasLocationSpecificInventory)
        {
            var locationInventory = LocationInventories.First(li => li.LocationId == locationId);
            locationInventory.RemainingQuantity--;
            locationInventory.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            RemainingQuantity--;
        }
        
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReleaseOne()
    {
        if (RemainingQuantity >= TotalQuantity)
            throw new InvalidOperationException("Cannot release more than total quantity");

        RemainingQuantity++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReleaseOneAtLocation(Guid locationId)
    {
        if (HasLocationSpecificInventory)
        {
            var locationInventory = LocationInventories.First(li => li.LocationId == locationId);
            if (locationInventory.RemainingQuantity >= locationInventory.TotalQuantity)
                throw new InvalidOperationException("Cannot release more than total quantity at location");

            locationInventory.RemainingQuantity++;
            locationInventory.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            if (RemainingQuantity >= TotalQuantity)
                throw new InvalidOperationException("Cannot release more than total quantity");
            RemainingQuantity++;
        }
        
        UpdatedAt = DateTime.UtcNow;
    }

    public decimal GetAvailabilityPercentage()
    {
        if (TotalQuantity == 0) return 0;
        return (decimal)RemainingQuantity / TotalQuantity * 100;
    }

    public decimal GetAvailabilityPercentageAtLocation(Guid locationId)
    {
        if (!HasLocationSpecificInventory)
            return GetAvailabilityPercentage();

        var locationInventory = LocationInventories.FirstOrDefault(li => li.LocationId == locationId);
        if (locationInventory == null || locationInventory.TotalQuantity == 0) return 0;
        
        return (decimal)locationInventory.RemainingQuantity / locationInventory.TotalQuantity * 100;
    }

    public void SetupLocationSpecificInventory(Dictionary<Guid, int> locationQuantities)
    {
        LocationInventories.Clear();
        foreach (var (locationId, quantity) in locationQuantities)
        {
            LocationInventories.Add(new PrizeLocationInventory
            {
                PrizeId = Id,
                LocationId = locationId,
                TotalQuantity = quantity,
                RemainingQuantity = quantity
            });
        }
        HasLocationSpecificInventory = true;
        TotalQuantity = locationQuantities.Values.Sum();
        RemainingQuantity = TotalQuantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public int GetRemainingQuantityAtLocation(Guid locationId)
    {
        if (!HasLocationSpecificInventory)
            return RemainingQuantity;

        var locationInventory = LocationInventories.FirstOrDefault(li => li.LocationId == locationId);
        return locationInventory?.RemainingQuantity ?? 0;
    }
}

public class PrizeLocationInventory
{
    public Guid PrizeId { get; set; }
    public Guid LocationId { get; set; }
    public int TotalQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Prize Prize { get; set; } = null!;
    public BusinessLocation Location { get; set; } = null!;
}