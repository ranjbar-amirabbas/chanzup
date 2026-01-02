using Microsoft.EntityFrameworkCore;
using Chanzup.Domain.Entities;
using Chanzup.Infrastructure.Services;

namespace Chanzup.Infrastructure.Extensions;

public static class MultiTenantExtensions
{
    public static void ApplyMultiTenantFilters(this ModelBuilder modelBuilder, ITenantContext tenantContext)
    {
        // Apply tenant filtering to business-related entities
        modelBuilder.Entity<Campaign>()
            .HasQueryFilter(e => tenantContext.TenantId == null || e.BusinessId == tenantContext.TenantId);

        modelBuilder.Entity<Prize>()
            .HasQueryFilter(e => tenantContext.TenantId == null || e.Campaign.BusinessId == tenantContext.TenantId);

        modelBuilder.Entity<BusinessLocation>()
            .HasQueryFilter(e => tenantContext.TenantId == null || e.BusinessId == tenantContext.TenantId);

        modelBuilder.Entity<Staff>()
            .HasQueryFilter(e => tenantContext.TenantId == null || e.BusinessId == tenantContext.TenantId);

        modelBuilder.Entity<QRSession>()
            .HasQueryFilter(e => tenantContext.TenantId == null || e.BusinessId == tenantContext.TenantId);

        modelBuilder.Entity<WheelSpin>()
            .HasQueryFilter(e => tenantContext.TenantId == null || e.Campaign.BusinessId == tenantContext.TenantId);
    }
}