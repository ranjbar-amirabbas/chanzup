using Microsoft.EntityFrameworkCore;
using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;

namespace Chanzup.Application.Services;

public interface IStaffLocationAccessService
{
    Task<bool> HasAccessToLocationAsync(Guid staffId, Guid locationId, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAtLocationAsync(Guid staffId, Guid locationId, LocationPermissions permission, CancellationToken cancellationToken = default);
    Task<List<BusinessLocation>> GetAccessibleLocationsAsync(Guid staffId, CancellationToken cancellationToken = default);
    Task GrantLocationAccessAsync(Guid staffId, Guid locationId, LocationPermissions permissions = LocationPermissions.All, CancellationToken cancellationToken = default);
    Task RevokeLocationAccessAsync(Guid staffId, Guid locationId, CancellationToken cancellationToken = default);
    Task SetAllLocationsAccessAsync(Guid staffId, CancellationToken cancellationToken = default);
    Task<List<Staff>> GetStaffWithLocationAccessAsync(Guid locationId, CancellationToken cancellationToken = default);
    Task<List<StaffLocationAccess>> GetStaffLocationAccessAsync(Guid businessId, CancellationToken cancellationToken = default);
}

public class StaffLocationAccessService : IStaffLocationAccessService
{
    private readonly IApplicationDbContext _context;

    public StaffLocationAccessService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasAccessToLocationAsync(Guid staffId, Guid locationId, CancellationToken cancellationToken = default)
    {
        var staff = await _context.Staff
            .Include(s => s.LocationAccess)
            .Include(s => s.Business)
            .ThenInclude(b => b.BusinessLocations)
            .FirstOrDefaultAsync(s => s.Id == staffId, cancellationToken);

        return staff?.HasAccessToLocation(locationId) ?? false;
    }

    public async Task<bool> HasPermissionAtLocationAsync(Guid staffId, Guid locationId, LocationPermissions permission, CancellationToken cancellationToken = default)
    {
        var staff = await _context.Staff
            .Include(s => s.LocationAccess)
            .Include(s => s.Business)
            .ThenInclude(b => b.BusinessLocations)
            .FirstOrDefaultAsync(s => s.Id == staffId, cancellationToken);

        return staff?.HasPermissionAtLocation(locationId, permission) ?? false;
    }

    public async Task<List<BusinessLocation>> GetAccessibleLocationsAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        var staff = await _context.Staff
            .Include(s => s.LocationAccess)
            .Include(s => s.Business)
            .ThenInclude(b => b.BusinessLocations)
            .FirstOrDefaultAsync(s => s.Id == staffId, cancellationToken);

        if (staff == null) return new List<BusinessLocation>();

        var accessibleLocationIds = staff.GetAccessibleLocationIds().ToList();
        
        return await _context.BusinessLocations
            .Where(bl => accessibleLocationIds.Contains(bl.Id) && bl.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task GrantLocationAccessAsync(Guid staffId, Guid locationId, LocationPermissions permissions = LocationPermissions.All, CancellationToken cancellationToken = default)
    {
        var staff = await _context.Staff
            .Include(s => s.LocationAccess)
            .Include(s => s.Business)
            .ThenInclude(b => b.BusinessLocations)
            .FirstOrDefaultAsync(s => s.Id == staffId, cancellationToken);

        if (staff == null)
            throw new ArgumentException($"Staff with ID {staffId} not found");

        // Verify the location belongs to the same business
        var location = await _context.BusinessLocations
            .FirstOrDefaultAsync(bl => bl.Id == locationId && bl.BusinessId == staff.BusinessId, cancellationToken);

        if (location == null)
            throw new ArgumentException($"Location with ID {locationId} not found or doesn't belong to staff's business");

        staff.GrantAccessToLocation(locationId, permissions);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeLocationAccessAsync(Guid staffId, Guid locationId, CancellationToken cancellationToken = default)
    {
        var staff = await _context.Staff
            .Include(s => s.LocationAccess)
            .FirstOrDefaultAsync(s => s.Id == staffId, cancellationToken);

        if (staff == null)
            throw new ArgumentException($"Staff with ID {staffId} not found");

        staff.RevokeAccessToLocation(locationId);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SetAllLocationsAccessAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        var staff = await _context.Staff
            .Include(s => s.LocationAccess)
            .Include(s => s.Business)
            .ThenInclude(b => b.BusinessLocations)
            .FirstOrDefaultAsync(s => s.Id == staffId, cancellationToken);

        if (staff == null)
            throw new ArgumentException($"Staff with ID {staffId} not found");

        staff.SetAllLocationsAccess();
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Staff>> GetStaffWithLocationAccessAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        return await _context.Staff
            .Include(s => s.LocationAccess)
            .Where(s => s.IsActive && 
                       (s.AccessLevel == StaffAccessLevel.AllLocations ||
                        s.LocationAccess.Any(la => la.LocationId == locationId && la.IsActive)))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<StaffLocationAccess>> GetStaffLocationAccessAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        return await _context.StaffLocationAccess
            .Include(sla => sla.Staff)
            .Include(sla => sla.Location)
            .Where(sla => sla.Staff.BusinessId == businessId && sla.IsActive)
            .ToListAsync(cancellationToken);
    }
}