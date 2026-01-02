namespace Chanzup.Application.DTOs;

public class LocationInventoryResponse
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public List<PrizeInventoryItem> PrizeInventories { get; set; } = new();
    public int TotalPrizes { get; set; }
    public int TotalRemainingQuantity { get; set; }
    public decimal InventoryUtilization { get; set; } // Percentage of inventory used
}

public class PrizeInventoryItem
{
    public Guid PrizeId { get; set; }
    public string PrizeName { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public decimal UtilizationRate { get; set; }
    public bool IsLowStock { get; set; } // Less than 20% remaining
    public bool IsOutOfStock { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class PrizeLocationInventoryResponse
{
    public Guid PrizeId { get; set; }
    public string PrizeName { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public bool HasLocationSpecificInventory { get; set; }
    public int TotalQuantityAcrossLocations { get; set; }
    public int RemainingQuantityAcrossLocations { get; set; }
    public List<LocationInventoryItem> LocationInventories { get; set; } = new();
}

public class LocationInventoryItem
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public decimal UtilizationRate { get; set; }
    public bool IsLowStock { get; set; }
    public bool IsOutOfStock { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class SetupLocationInventoryRequest
{
    public Guid PrizeId { get; set; }
    public Dictionary<Guid, int> LocationQuantities { get; set; } = new();
}

public class UpdateLocationInventoryRequest
{
    public Guid PrizeId { get; set; }
    public Guid LocationId { get; set; }
    public int NewQuantity { get; set; }
}

public class TransferInventoryRequest
{
    public Guid PrizeId { get; set; }
    public Guid FromLocationId { get; set; }
    public Guid ToLocationId { get; set; }
    public int Quantity { get; set; }
    public string? Reason { get; set; }
}

public class InventoryTransferResponse
{
    public Guid TransferId { get; set; }
    public Guid PrizeId { get; set; }
    public string PrizeName { get; set; } = string.Empty;
    public Guid FromLocationId { get; set; }
    public string FromLocationName { get; set; } = string.Empty;
    public Guid ToLocationId { get; set; }
    public string ToLocationName { get; set; } = string.Empty;
    public int QuantityTransferred { get; set; }
    public string? Reason { get; set; }
    public DateTime TransferredAt { get; set; }
    public LocationInventoryItem FromLocationAfterTransfer { get; set; } = new();
    public LocationInventoryItem ToLocationAfterTransfer { get; set; } = new();
}

public class InventoryAlertResponse
{
    public List<LowStockAlert> LowStockAlerts { get; set; } = new();
    public List<OutOfStockAlert> OutOfStockAlerts { get; set; } = new();
    public List<OverstockAlert> OverstockAlerts { get; set; } = new();
}

public class LowStockAlert
{
    public Guid PrizeId { get; set; }
    public string PrizeName { get; set; } = string.Empty;
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int RemainingQuantity { get; set; }
    public int TotalQuantity { get; set; }
    public decimal UtilizationRate { get; set; }
    public string Severity { get; set; } = string.Empty; // Low, Critical
}

public class OutOfStockAlert
{
    public Guid PrizeId { get; set; }
    public string PrizeName { get; set; } = string.Empty;
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public DateTime OutOfStockSince { get; set; }
    public int TotalQuantity { get; set; }
}

public class OverstockAlert
{
    public Guid PrizeId { get; set; }
    public string PrizeName { get; set; } = string.Empty;
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int RemainingQuantity { get; set; }
    public int TotalQuantity { get; set; }
    public decimal UtilizationRate { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}