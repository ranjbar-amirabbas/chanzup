using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Chanzup.Domain.Entities;

namespace Chanzup.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Business> Businesses { get; }
    DbSet<Player> Players { get; }
    DbSet<Campaign> Campaigns { get; }
    DbSet<Prize> Prizes { get; }
    DbSet<QRSession> QRSessions { get; }
    DbSet<WheelSpin> WheelSpins { get; }
    DbSet<BusinessLocation> BusinessLocations { get; }
    DbSet<Staff> Staff { get; }
    DbSet<PlayerPrize> PlayerPrizes { get; }
    DbSet<TokenTransaction> TokenTransactions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Referral> Referrals { get; }
    DbSet<SocialShare> SocialShares { get; }
    DbSet<AnalyticsEvent> AnalyticsEvents { get; }
    DbSet<CampaignLocation> CampaignLocations { get; }
    DbSet<PrizeLocationInventory> PrizeLocationInventories { get; }
    DbSet<StaffLocationAccess> StaffLocationAccess { get; }
    DbSet<Admin> Admins { get; }
    DbSet<BusinessApplication> BusinessApplications { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<DisputeResolution> DisputeResolutions { get; }
    DbSet<DisputeMessage> DisputeMessages { get; }
    DbSet<SystemParameter> SystemParameters { get; }
    DbSet<SuspiciousActivity> SuspiciousActivities { get; }

    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}