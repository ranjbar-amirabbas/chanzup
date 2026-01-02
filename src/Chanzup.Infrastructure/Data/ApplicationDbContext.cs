using Microsoft.EntityFrameworkCore;
using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;
using Chanzup.Domain.ValueObjects;
using Chanzup.Infrastructure.Services;
using Chanzup.Infrastructure.Extensions;

namespace Chanzup.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantContext _tenantContext;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<Prize> Prizes => Set<Prize>();
    public DbSet<QRSession> QRSessions => Set<QRSession>();
    public DbSet<WheelSpin> WheelSpins => Set<WheelSpin>();
    public DbSet<BusinessLocation> BusinessLocations => Set<BusinessLocation>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<PlayerPrize> PlayerPrizes => Set<PlayerPrize>();
    public DbSet<TokenTransaction> TokenTransactions => Set<TokenTransaction>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Referral> Referrals => Set<Referral>();
    public DbSet<SocialShare> SocialShares => Set<SocialShare>();
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();
    public DbSet<CampaignLocation> CampaignLocations => Set<CampaignLocation>();
    public DbSet<PrizeLocationInventory> PrizeLocationInventories => Set<PrizeLocationInventory>();
    public DbSet<StaffLocationAccess> StaffLocationAccess => Set<StaffLocationAccess>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<BusinessApplication> BusinessApplications => Set<BusinessApplication>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<DisputeResolution> DisputeResolutions => Set<DisputeResolution>();
    public DbSet<DisputeMessage> DisputeMessages => Set<DisputeMessage>();
    public DbSet<SystemParameter> SystemParameters => Set<SystemParameter>();
    public DbSet<SuspiciousActivity> SuspiciousActivities => Set<SuspiciousActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure value object conversions
        ConfigureValueObjects(modelBuilder);
        
        // Configure entities
        ConfigureBusinessEntity(modelBuilder);
        ConfigurePlayerEntity(modelBuilder);
        ConfigureCampaignEntity(modelBuilder);
        ConfigurePrizeEntity(modelBuilder);
        ConfigureQRSessionEntity(modelBuilder);
        ConfigureWheelSpinEntity(modelBuilder);
        ConfigureBusinessLocationEntity(modelBuilder);
        ConfigureStaffEntity(modelBuilder);
        ConfigurePlayerPrizeEntity(modelBuilder);
        ConfigureTokenTransactionEntity(modelBuilder);
        ConfigureRefreshTokenEntity(modelBuilder);
        ConfigureReferralEntity(modelBuilder);
        ConfigureSocialShareEntity(modelBuilder);
        ConfigureAnalyticsEventEntity(modelBuilder);
        ConfigureCampaignLocationEntity(modelBuilder);
        ConfigurePrizeLocationInventoryEntity(modelBuilder);
        ConfigureStaffLocationAccessEntity(modelBuilder);
        ConfigureAdminEntity(modelBuilder);
        ConfigureBusinessApplicationEntity(modelBuilder);
        ConfigureAuditLogEntity(modelBuilder);
        ConfigureDisputeResolutionEntity(modelBuilder);
        ConfigureDisputeMessageEntity(modelBuilder);
        ConfigureSystemParameterEntity(modelBuilder);
        ConfigureSuspiciousActivityEntity(modelBuilder);

        // Apply multi-tenant filters
        modelBuilder.ApplyMultiTenantFilters(_tenantContext);
    }

    private void ConfigureValueObjects(ModelBuilder modelBuilder)
    {
        // Email value object conversion
        modelBuilder.Entity<Business>()
            .Property(e => e.Email)
            .HasConversion(
                email => email.Value,
                value => new Email(value))
            .HasMaxLength(255);

        modelBuilder.Entity<Player>()
            .Property(e => e.Email)
            .HasConversion(
                email => email.Value,
                value => new Email(value))
            .HasMaxLength(255);

        modelBuilder.Entity<Staff>()
            .Property(e => e.Email)
            .HasConversion(
                email => email.Value,
                value => new Email(value))
            .HasMaxLength(255);

        modelBuilder.Entity<Admin>()
            .Property(e => e.Email)
            .HasConversion(
                email => email.Value,
                value => new Email(value))
            .HasMaxLength(255);

        modelBuilder.Entity<BusinessApplication>()
            .Property(e => e.Email)
            .HasConversion(
                email => email.Value,
                value => new Email(value))
            .HasMaxLength(255);

        // Location value object conversion for Business
        modelBuilder.Entity<Business>()
            .OwnsOne(e => e.Location, location =>
            {
                location.Property(l => l.Latitude)
                    .HasColumnName("Latitude")
                    .HasPrecision(10, 8);
                location.Property(l => l.Longitude)
                    .HasColumnName("Longitude")
                    .HasPrecision(11, 8);
            });

        // Location value object conversion for BusinessLocation
        modelBuilder.Entity<BusinessLocation>()
            .OwnsOne(e => e.Location, location =>
            {
                location.Property(l => l.Latitude)
                    .HasColumnName("Latitude")
                    .HasPrecision(10, 8);
                location.Property(l => l.Longitude)
                    .HasColumnName("Longitude")
                    .HasPrecision(11, 8);
            });

        // Location value object conversion for BusinessApplication
        modelBuilder.Entity<BusinessApplication>()
            .OwnsOne(e => e.Location, location =>
            {
                location.Property(l => l.Latitude)
                    .HasColumnName("Latitude")
                    .HasPrecision(10, 8);
                location.Property(l => l.Longitude)
                    .HasColumnName("Longitude")
                    .HasPrecision(11, 8);
            });

        // Location value object conversion for QRSession
        modelBuilder.Entity<QRSession>()
            .OwnsOne(e => e.PlayerLocation, location =>
            {
                location.Property(l => l.Latitude)
                    .HasColumnName("PlayerLatitude")
                    .HasPrecision(10, 8);
                location.Property(l => l.Longitude)
                    .HasColumnName("PlayerLongitude")
                    .HasPrecision(11, 8);
            });

        // Money value object conversion
        modelBuilder.Entity<Prize>()
            .OwnsOne(e => e.Value, money =>
            {
                money.Property(m => m.Amount)
                    .HasColumnName("Value")
                    .HasPrecision(10, 2);
                money.Property(m => m.Currency)
                    .HasColumnName("Currency")
                    .HasMaxLength(3)
                    .HasDefaultValue("CAD");
            });

        // RedemptionCode value object conversion
        modelBuilder.Entity<PlayerPrize>()
            .Property(e => e.RedemptionCode)
            .HasConversion(
                code => code.Value,
                value => new RedemptionCode(value))
            .HasMaxLength(20);
    }

    private void ConfigureBusinessEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Business>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Address).HasMaxLength(500);
            
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => new { e.IsActive, e.SubscriptionTier });
            
            // Multi-tenant support - add tenant filtering if needed
            entity.HasQueryFilter(b => b.IsActive);
        });
    }

    private void ConfigurePlayerEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(50);
            
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });
    }

    private void ConfigureCampaignEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            
            entity.HasOne(e => e.Business)
                .WithMany(e => e.Campaigns)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.BusinessId, e.IsActive });
            entity.HasIndex(e => new { e.IsActive, e.StartDate, e.EndDate });
        });
    }

    private void ConfigurePrizeEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Prize>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.WinProbability).HasPrecision(5, 4);
            
            entity.HasOne(e => e.Campaign)
                .WithMany(e => e.Prizes)
                .HasForeignKey(e => e.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.CampaignId, e.IsActive });
            entity.HasIndex(e => new { e.RemainingQuantity, e.IsActive });
        });
    }

    private void ConfigureQRSessionEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QRSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionHash).IsRequired().HasMaxLength(255);
            
            entity.HasOne(e => e.Player)
                .WithMany(e => e.QRSessions)
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Business)
                .WithMany()
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.PlayerId, e.CreatedAt });
            entity.HasIndex(e => new { e.BusinessId, e.CreatedAt });
            entity.HasIndex(e => e.SessionHash).IsUnique();
        });
    }

    private void ConfigureWheelSpinEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WheelSpin>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SpinResult).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RandomSeed).IsRequired().HasMaxLength(255);
            
            entity.HasOne(e => e.Player)
                .WithMany(e => e.WheelSpins)
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Campaign)
                .WithMany(e => e.WheelSpins)
                .HasForeignKey(e => e.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Prize)
                .WithMany(e => e.WheelSpins)
                .HasForeignKey(e => e.PrizeId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => new { e.PlayerId, e.CreatedAt });
            entity.HasIndex(e => new { e.CampaignId, e.CreatedAt });
        });
    }

    private void ConfigureBusinessLocationEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BusinessLocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
            entity.Property(e => e.QRCode).IsRequired().HasMaxLength(255);
            
            entity.HasOne(e => e.Business)
                .WithMany(e => e.BusinessLocations)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.BusinessId, e.IsActive });
            entity.HasIndex(e => e.QRCode).IsUnique();
        });
    }

    private void ConfigureStaffEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            
            entity.HasOne(e => e.Business)
                .WithMany(e => e.Staff)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.BusinessId, e.Email }).IsUnique();
            entity.HasIndex(e => new { e.BusinessId, e.IsActive });
        });
    }

    private void ConfigurePlayerPrizeEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerPrize>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Player)
                .WithMany(e => e.PlayerPrizes)
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Prize)
                .WithMany(e => e.PlayerPrizes)
                .HasForeignKey(e => e.PrizeId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => e.RedemptionCode).IsUnique();
            entity.HasIndex(e => new { e.PlayerId, e.IsRedeemed });
            entity.HasIndex(e => new { e.ExpiresAt, e.IsRedeemed });
        });
    }

    private void ConfigureTokenTransactionEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TokenTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            
            entity.HasOne(e => e.Player)
                .WithMany(e => e.TokenTransactions)
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.PlayerId, e.CreatedAt });
            entity.HasIndex(e => new { e.Type, e.CreatedAt });
        });
    }

    private void ConfigureRefreshTokenEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(255);
            entity.Property(e => e.UserType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RevokedByIp).HasMaxLength(50);
            entity.Property(e => e.ReplacedByToken).HasMaxLength(255);
            
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.UserType, e.IsRevoked });
            entity.HasIndex(e => new { e.ExpiresAt, e.IsRevoked });
        });
    }

    private void ConfigureReferralEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Referral>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReferralCode).IsRequired().HasMaxLength(20);
            
            entity.HasOne(e => e.Referrer)
                .WithMany(e => e.ReferralsMade)
                .HasForeignKey(e => e.ReferrerId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.ReferredPlayer)
                .WithMany(e => e.ReferralsReceived)
                .HasForeignKey(e => e.ReferredPlayerId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasIndex(e => e.ReferralCode).IsUnique();
            entity.HasIndex(e => new { e.ReferrerId, e.IsCompleted });
            entity.HasIndex(e => new { e.ReferredPlayerId, e.IsCompleted });
        });
    }

    private void ConfigureSocialShareEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SocialShare>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ExternalShareId).HasMaxLength(255);
            
            entity.HasOne(e => e.Player)
                .WithMany(e => e.SocialShares)
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.PlayerId, e.CreatedAt });
            entity.HasIndex(e => new { e.Platform, e.IsVerified });
            entity.HasIndex(e => e.ExternalShareId);
        });
    }

    private void ConfigureAnalyticsEventEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnalyticsEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EventData).IsRequired();
            
            entity.HasOne(e => e.Business)
                .WithMany()
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.NoAction);
                
            entity.HasOne(e => e.Player)
                .WithMany()
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(e => e.Campaign)
                .WithMany()
                .HasForeignKey(e => e.CampaignId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => new { e.BusinessId, e.EventType, e.CreatedAt });
            entity.HasIndex(e => new { e.PlayerId, e.EventType, e.CreatedAt });
            entity.HasIndex(e => new { e.CampaignId, e.EventType, e.CreatedAt });
            entity.HasIndex(e => new { e.EventType, e.CreatedAt });
        });
    }

    private void ConfigureCampaignLocationEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CampaignLocation>(entity =>
        {
            entity.HasKey(e => new { e.CampaignId, e.LocationId });
            
            entity.HasOne(e => e.Campaign)
                .WithMany(e => e.CampaignLocations)
                .HasForeignKey(e => e.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Location)
                .WithMany()
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => e.CampaignId);
            entity.HasIndex(e => e.LocationId);
        });
    }

    private void ConfigurePrizeLocationInventoryEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PrizeLocationInventory>(entity =>
        {
            entity.HasKey(e => new { e.PrizeId, e.LocationId });
            
            entity.HasOne(e => e.Prize)
                .WithMany(e => e.LocationInventories)
                .HasForeignKey(e => e.PrizeId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Location)
                .WithMany()
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => e.PrizeId);
            entity.HasIndex(e => e.LocationId);
            entity.HasIndex(e => new { e.LocationId, e.RemainingQuantity });
        });
    }

    private void ConfigureStaffLocationAccessEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StaffLocationAccess>(entity =>
        {
            entity.HasKey(e => new { e.StaffId, e.LocationId });
            
            entity.HasOne(e => e.Staff)
                .WithMany(e => e.LocationAccess)
                .HasForeignKey(e => e.StaffId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Location)
                .WithMany()
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => e.StaffId);
            entity.HasIndex(e => e.LocationId);
            entity.HasIndex(e => new { e.StaffId, e.IsActive });
            entity.HasIndex(e => new { e.LocationId, e.IsActive });
        });
    }

    private void ConfigureAdminEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => new { e.IsActive, e.Role });
        });
    }

    private void ConfigureBusinessApplicationEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BusinessApplication>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BusinessName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.BusinessDescription).HasMaxLength(2000);
            entity.Property(e => e.BusinessCategory).HasMaxLength(100);
            entity.Property(e => e.Website).HasMaxLength(500);
            entity.Property(e => e.SocialMediaLinks).HasMaxLength(1000);
            entity.Property(e => e.ReviewNotes).HasMaxLength(1000);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);
            
            entity.HasOne(e => e.Reviewer)
                .WithMany(e => e.ReviewedApplications)
                .HasForeignKey(e => e.ReviewedBy)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(e => e.Business)
                .WithMany()
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => new { e.Status, e.SubmittedAt });
            entity.HasIndex(e => new { e.ReviewedBy, e.ReviewedAt });
        });
    }

    private void ConfigureAuditLogEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UserType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UserEmail).HasMaxLength(255);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.OldValues).HasMaxLength(4000);
            entity.Property(e => e.NewValues).HasMaxLength(4000);
            entity.Property(e => e.AdditionalData).HasMaxLength(2000);
            
            entity.HasOne(e => e.Admin)
                .WithMany(e => e.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => new { e.Action, e.CreatedAt });
            entity.HasIndex(e => new { e.EntityType, e.EntityId, e.CreatedAt });
            entity.HasIndex(e => new { e.UserType, e.UserId, e.CreatedAt });
            entity.HasIndex(e => new { e.UserEmail, e.CreatedAt });
        });
    }

    private void ConfigureDisputeResolutionEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DisputeResolution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.ReporterType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ReporterEmail).HasMaxLength(255);
            entity.Property(e => e.SubjectType).HasMaxLength(100);
            entity.Property(e => e.Resolution).HasMaxLength(2000);
            entity.Property(e => e.AdminNotes).HasMaxLength(1000);
            
            entity.HasOne(e => e.AssignedAdmin)
                .WithMany(e => e.DisputeResolutions)
                .HasForeignKey(e => e.AssignedTo)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => new { e.Status, e.Priority, e.CreatedAt });
            entity.HasIndex(e => new { e.AssignedTo, e.Status });
            entity.HasIndex(e => new { e.Type, e.Status });
            entity.HasIndex(e => new { e.ReporterId, e.ReporterType });
        });
    }

    private void ConfigureDisputeMessageEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DisputeMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.SenderType).IsRequired().HasMaxLength(50);
            
            entity.HasOne(e => e.Dispute)
                .WithMany(e => e.Messages)
                .HasForeignKey(e => e.DisputeId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.DisputeId, e.CreatedAt });
            entity.HasIndex(e => new { e.SenderId, e.SenderType });
        });
    }

    private void ConfigureSystemParameterEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemParameter>(entity =>
        {
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ValidationRule).HasMaxLength(500);
            
            entity.HasOne(e => e.UpdatedByAdmin)
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => new { e.Category, e.Key });
        });
    }

    private void ConfigureSuspiciousActivityEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SuspiciousActivity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ActivityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.UserType).HasMaxLength(50);
            entity.Property(e => e.UserEmail).HasMaxLength(255);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.ActivityData).IsRequired();
            entity.Property(e => e.RiskScore).HasPrecision(5, 2);
            entity.Property(e => e.ReviewNotes).HasMaxLength(1000);
            entity.Property(e => e.ActionTaken).HasMaxLength(500);
            
            entity.HasOne(e => e.ReviewedByAdmin)
                .WithMany()
                .HasForeignKey(e => e.ReviewedBy)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => new { e.Status, e.Severity, e.DetectedAt });
            entity.HasIndex(e => new { e.UserId, e.UserType, e.DetectedAt });
            entity.HasIndex(e => new { e.ActivityType, e.DetectedAt });
            entity.HasIndex(e => new { e.ReviewedBy, e.ReviewedAt });
        });
    }
}