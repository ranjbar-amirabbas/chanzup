namespace Chanzup.Domain.Entities;

public class Campaign
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GameType GameType { get; set; } = GameType.WheelOfLuck;
    public int TokenCostPerSpin { get; set; } = 1;
    public int MaxSpinsPerDay { get; set; } = 10;
    public bool IsActive { get; set; } = true;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public CampaignTargeting Targeting { get; set; } = CampaignTargeting.AllLocations;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Business Business { get; set; } = null!;
    public ICollection<Prize> Prizes { get; set; } = new List<Prize>();
    public ICollection<WheelSpin> WheelSpins { get; set; } = new List<WheelSpin>();
    public ICollection<CampaignLocation> CampaignLocations { get; set; } = new List<CampaignLocation>();

    // Domain methods
    public bool IsCurrentlyActive()
    {
        var now = DateTime.UtcNow;
        return IsActive && 
               StartDate <= now && 
               (EndDate == null || EndDate >= now);
    }

    public bool CanPlayerSpin(Player player)
    {
        if (!IsCurrentlyActive() || !player.CanAffordSpin(TokenCostPerSpin))
            return false;

        var today = DateTime.UtcNow.Date;
        var todaySpins = WheelSpins.Count(s => s.PlayerId == player.Id && s.CreatedAt.Date == today);
        
        return todaySpins < MaxSpinsPerDay;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public IEnumerable<Prize> GetAvailablePrizes()
    {
        return Prizes.Where(p => p.IsAvailable());
    }

    public decimal GetTotalProbability()
    {
        return Prizes.Where(p => p.IsAvailable()).Sum(p => p.WinProbability);
    }

    public bool IsAvailableAtLocation(Guid locationId)
    {
        if (!IsCurrentlyActive()) return false;
        
        return Targeting switch
        {
            CampaignTargeting.AllLocations => true,
            CampaignTargeting.SpecificLocations => CampaignLocations.Any(cl => cl.LocationId == locationId),
            _ => false
        };
    }

    public void SetLocationTargeting(IEnumerable<Guid> locationIds)
    {
        CampaignLocations.Clear();
        foreach (var locationId in locationIds)
        {
            CampaignLocations.Add(new CampaignLocation { CampaignId = Id, LocationId = locationId });
        }
        Targeting = CampaignTargeting.SpecificLocations;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAllLocationsTargeting()
    {
        CampaignLocations.Clear();
        Targeting = CampaignTargeting.AllLocations;
        UpdatedAt = DateTime.UtcNow;
    }

    public IEnumerable<Guid> GetTargetedLocationIds()
    {
        return Targeting == CampaignTargeting.AllLocations 
            ? Business.BusinessLocations.Where(bl => bl.IsActive).Select(bl => bl.Id)
            : CampaignLocations.Select(cl => cl.LocationId);
    }
}

public enum GameType
{
    WheelOfLuck = 0,
    TreasureHunt = 1
}

public enum CampaignTargeting
{
    AllLocations = 0,
    SpecificLocations = 1
}

public class CampaignLocation
{
    public Guid CampaignId { get; set; }
    public Guid LocationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Campaign Campaign { get; set; } = null!;
    public BusinessLocation Location { get; set; } = null!;
}