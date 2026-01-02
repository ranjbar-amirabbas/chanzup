using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Chanzup.Application.DTOs;
using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;
using Chanzup.Domain.ValueObjects;

namespace Chanzup.Application.Services;

public class AdminManagementService : IAdminManagementService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AdminManagementService> _logger;

    public AdminManagementService(IApplicationDbContext context, ILogger<AdminManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Admin Management
    public async Task<AdminResponse> CreateAdminAsync(RegisterAdminRequest request, Guid createdBy)
    {
        // Check if admin already exists
        var existingAdmin = await _context.Admins
            .FirstOrDefaultAsync(a => a.Email.Value == request.Email);
        if (existingAdmin != null)
        {
            throw new InvalidOperationException("Admin with this email already exists");
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Create admin
        var admin = new Admin
        {
            Email = new Email(request.Email),
            PasswordHash = passwordHash,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            CreatedBy = createdBy
        };

        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        // Log the action
        await LogActionAsync("ADMIN_CREATED", "Admin", admin.Id, createdBy, "Admin", 
            additionalData: $"Created admin: {admin.Email.Value} with role: {admin.Role}");

        return MapToAdminResponse(admin);
    }

    public async Task<AdminResponse> GetAdminAsync(Guid adminId)
    {
        var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.Id == adminId);

        if (admin == null)
            throw new ArgumentException("Admin not found");

        return MapToAdminResponse(admin);
    }

    public async Task<IEnumerable<AdminResponse>> GetAllAdminsAsync()
    {
        var admins = await _context.Admins
            .OrderBy(a => a.FirstName)
            .ThenBy(a => a.LastName)
            .ToListAsync();

        return admins.Select(MapToAdminResponse);
    }

    public async Task<AdminResponse> UpdateAdminAsync(Guid adminId, UpdateAdminRequest request)
    {
        var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.Id == adminId);

        if (admin == null)
            throw new ArgumentException("Admin not found");

        var oldValues = $"Role: {admin.Role}, IsActive: {admin.IsActive}";

        // Update fields
        if (!string.IsNullOrEmpty(request.FirstName))
            admin.FirstName = request.FirstName;
        
        if (!string.IsNullOrEmpty(request.LastName))
            admin.LastName = request.LastName;
        
        if (request.Role.HasValue)
            admin.Role = request.Role.Value;
        
        if (request.IsActive.HasValue)
            admin.IsActive = request.IsActive.Value;

        admin.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var newValues = $"Role: {admin.Role}, IsActive: {admin.IsActive}";

        // Log the action
        await LogActionAsync("ADMIN_UPDATED", "Admin", admin.Id, null, "Admin", 
            oldValues: oldValues, newValues: newValues);

        return MapToAdminResponse(admin);
    }

    public async Task DeleteAdminAsync(Guid adminId)
    {
        var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.Id == adminId);

        if (admin == null)
            throw new ArgumentException("Admin not found");

        // Soft delete by deactivating
        admin.IsActive = false;
        admin.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Log the action
        await LogActionAsync("ADMIN_DELETED", "Admin", admin.Id, null, "Admin", 
            additionalData: $"Deactivated admin: {admin.Email.Value}");
    }

    // Business Application Management
    public async Task<IEnumerable<BusinessApplicationResponse>> GetPendingApplicationsAsync()
    {
        var applications = await _context.BusinessApplications
            .Include(ba => ba.Reviewer)
            .Where(ba => ba.Status == ApplicationStatus.Pending)
            .OrderBy(ba => ba.SubmittedAt)
            .ToListAsync();

        return applications.Select(MapToBusinessApplicationResponse);
    }

    public async Task<BusinessApplicationResponse> GetApplicationAsync(Guid applicationId)
    {
        var application = await _context.BusinessApplications
            .Include(ba => ba.Reviewer)
            .FirstOrDefaultAsync(ba => ba.Id == applicationId);

        if (application == null)
            throw new ArgumentException("Application not found");

        return MapToBusinessApplicationResponse(application);
    }

    public async Task<BusinessApplicationResponse> ReviewApplicationAsync(Guid applicationId, ReviewBusinessApplicationRequest request, Guid reviewerId)
    {
        var application = await _context.BusinessApplications
            .FirstOrDefaultAsync(ba => ba.Id == applicationId);

        if (application == null)
            throw new ArgumentException("Application not found");

        if (!application.CanBeReviewed())
            throw new InvalidOperationException("Application cannot be reviewed in current status");

        switch (request.Status)
        {
            case ApplicationStatus.Approved:
                application.Approve(reviewerId, request.ReviewNotes);
                break;
            case ApplicationStatus.Rejected:
                application.Reject(reviewerId, request.RejectionReason ?? "No reason provided", request.ReviewNotes);
                break;
            case ApplicationStatus.MoreInfoRequired:
                application.RequestMoreInfo(reviewerId, request.ReviewNotes ?? "More information required");
                break;
            default:
                throw new ArgumentException("Invalid status for review");
        }

        await _context.SaveChangesAsync();

        // Log the action
        await LogActionAsync("APPLICATION_REVIEWED", "BusinessApplication", application.Id, reviewerId, "Admin", 
            additionalData: $"Status: {request.Status}, Business: {application.BusinessName}");

        return await GetApplicationAsync(applicationId);
    }

    public async Task<Business> ApproveApplicationAsync(Guid applicationId, Guid reviewerId)
    {
        var application = await _context.BusinessApplications
            .FirstOrDefaultAsync(ba => ba.Id == applicationId);

        if (application == null)
            throw new ArgumentException("Application not found");

        if (application.Status != ApplicationStatus.Approved)
            throw new InvalidOperationException("Application must be approved before creating business");

        // Create business from application
        var business = new Business
        {
            Name = application.BusinessName,
            Email = application.Email,
            Phone = application.Phone,
            Address = application.Address,
            Location = application.Location,
            SubscriptionTier = application.RequestedTier
        };

        _context.Businesses.Add(business);
        
        // Link application to business
        application.BusinessId = business.Id;
        
        await _context.SaveChangesAsync();

        // Log the action
        await LogActionAsync("BUSINESS_CREATED_FROM_APPLICATION", "Business", business.Id, reviewerId, "Admin", 
            additionalData: $"Created from application: {applicationId}");

        return business;
    }

    // Account Management
    public async Task<AccountSuspensionResponse> SuspendBusinessAsync(Guid businessId, SuspendAccountRequest request, Guid suspendedBy)
    {
        var business = await _context.Businesses
            .FirstOrDefaultAsync(b => b.Id == businessId);

        if (business == null)
            throw new ArgumentException("Business not found");

        business.IsActive = false;
        business.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Log the action
        await LogActionAsync("BUSINESS_SUSPENDED", "Business", business.Id, suspendedBy, "Admin", 
            additionalData: $"Reason: {request.Reason}, Permanent: {request.IsPermanent}");

        return new AccountSuspensionResponse
        {
            AccountId = business.Id,
            AccountType = "Business",
            Email = business.Email.Value,
            IsSuspended = true,
            SuspensionReason = request.Reason,
            SuspendedAt = DateTime.UtcNow,
            SuspensionEndDate = request.SuspensionEndDate
        };
    }

    public async Task<AccountSuspensionResponse> SuspendPlayerAsync(Guid playerId, SuspendAccountRequest request, Guid suspendedBy)
    {
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.Id == playerId);

        if (player == null)
            throw new ArgumentException("Player not found");

        player.IsActive = false;
        player.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Log the action
        await LogActionAsync("PLAYER_SUSPENDED", "Player", player.Id, suspendedBy, "Admin", 
            additionalData: $"Reason: {request.Reason}, Permanent: {request.IsPermanent}");

        return new AccountSuspensionResponse
        {
            AccountId = player.Id,
            AccountType = "Player",
            Email = player.Email.Value,
            IsSuspended = true,
            SuspensionReason = request.Reason,
            SuspendedAt = DateTime.UtcNow,
            SuspensionEndDate = request.SuspensionEndDate
        };
    }

    public async Task<AccountSuspensionResponse> UnsuspendBusinessAsync(Guid businessId, Guid unsuspendedBy)
    {
        var business = await _context.Businesses
            .FirstOrDefaultAsync(b => b.Id == businessId);

        if (business == null)
            throw new ArgumentException("Business not found");

        business.IsActive = true;
        business.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Log the action
        await LogActionAsync("BUSINESS_UNSUSPENDED", "Business", business.Id, unsuspendedBy, "Admin");

        return new AccountSuspensionResponse
        {
            AccountId = business.Id,
            AccountType = "Business",
            Email = business.Email.Value,
            IsSuspended = false
        };
    }

    public async Task<AccountSuspensionResponse> UnsuspendPlayerAsync(Guid playerId, Guid unsuspendedBy)
    {
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.Id == playerId);

        if (player == null)
            throw new ArgumentException("Player not found");

        player.IsActive = true;
        player.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Log the action
        await LogActionAsync("PLAYER_UNSUSPENDED", "Player", player.Id, unsuspendedBy, "Admin");

        return new AccountSuspensionResponse
        {
            AccountId = player.Id,
            AccountType = "Player",
            Email = player.Email.Value,
            IsSuspended = false
        };
    }

    public async Task BanAccountAsync(Guid accountId, string accountType, string reason, Guid bannedBy)
    {
        if (accountType.Equals("Business", StringComparison.OrdinalIgnoreCase))
        {
            var business = await _context.Businesses.FindAsync(accountId);
            if (business != null)
            {
                business.SubscriptionTier = SubscriptionTier.Suspended;
                business.IsActive = false;
                business.UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (accountType.Equals("Player", StringComparison.OrdinalIgnoreCase))
        {
            var player = await _context.Players.FindAsync(accountId);
            if (player != null)
            {
                player.IsActive = false;
                player.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        // Log the action
        await LogActionAsync($"{accountType.ToUpper()}_BANNED", accountType, accountId, bannedBy, "Admin", 
            additionalData: $"Reason: {reason}");
    }

    // Dispute Resolution
    public async Task<DisputeResponse> CreateDisputeAsync(CreateDisputeRequest request, Guid reporterId, string reporterType, string reporterEmail)
    {
        var dispute = new DisputeResolution
        {
            Title = request.Title,
            Description = request.Description,
            Type = request.Type,
            Priority = request.Priority,
            ReporterId = reporterId,
            ReporterType = reporterType,
            ReporterEmail = reporterEmail,
            SubjectId = request.SubjectId,
            SubjectType = request.SubjectType
        };

        _context.DisputeResolutions.Add(dispute);
        await _context.SaveChangesAsync();

        // Log the action
        await LogActionAsync("DISPUTE_CREATED", "DisputeResolution", dispute.Id, reporterId, reporterType, 
            additionalData: $"Type: {request.Type}, Priority: {request.Priority}");

        return MapToDisputeResponse(dispute);
    }

    public async Task<IEnumerable<DisputeResponse>> GetDisputesAsync(DisputeStatus? status = null, Guid? assignedTo = null)
    {
        var query = _context.DisputeResolutions
            .Include(d => d.AssignedAdmin)
            .Include(d => d.Messages)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);

        if (assignedTo.HasValue)
            query = query.Where(d => d.AssignedTo == assignedTo.Value);

        var disputes = await query
            .OrderByDescending(d => d.Priority)
            .ThenBy(d => d.CreatedAt)
            .ToListAsync();

        return disputes.Select(MapToDisputeResponse);
    }

    public async Task<DisputeResponse> GetDisputeAsync(Guid disputeId)
    {
        var dispute = await _context.DisputeResolutions
            .Include(d => d.AssignedAdmin)
            .Include(d => d.Messages)
            .FirstOrDefaultAsync(d => d.Id == disputeId);

        if (dispute == null)
            throw new ArgumentException("Dispute not found");

        return MapToDisputeResponse(dispute);
    }

    public async Task<DisputeResponse> AssignDisputeAsync(Guid disputeId, Guid adminId)
    {
        var dispute = await _context.DisputeResolutions
            .FirstOrDefaultAsync(d => d.Id == disputeId);

        if (dispute == null)
            throw new ArgumentException("Dispute not found");

        dispute.AssignTo(adminId);
        await _context.SaveChangesAsync();

        // Log the action
        await LogActionAsync("DISPUTE_ASSIGNED", "DisputeResolution", dispute.Id, adminId, "Admin");

        return await GetDisputeAsync(disputeId);
    }

    public async Task<DisputeResponse> ResolveDisputeAsync(Guid disputeId, ResolveDisputeRequest request)
    {
        var dispute = await _context.DisputeResolutions
            .FirstOrDefaultAsync(d => d.Id == disputeId);

        if (dispute == null)
            throw new ArgumentException("Dispute not found");

        dispute.Resolve(request.Resolution, request.AdminNotes);
        await _context.SaveChangesAsync();

        // Log the action
        await LogActionAsync("DISPUTE_RESOLVED", "DisputeResolution", dispute.Id, dispute.AssignedTo, "Admin");

        return await GetDisputeAsync(disputeId);
    }

    public async Task AddDisputeMessageAsync(Guid disputeId, AddDisputeMessageRequest request, Guid senderId, string senderType)
    {
        var dispute = await _context.DisputeResolutions
            .FirstOrDefaultAsync(d => d.Id == disputeId);

        if (dispute == null)
            throw new ArgumentException("Dispute not found");

        dispute.AddMessage(request.Content, senderId, senderType);
        await _context.SaveChangesAsync();
    }

    // Audit Logging
    public async Task LogActionAsync(string action, string entityType, Guid? entityId, Guid? userId, string userType, 
        string? userEmail = null, string? oldValues = null, string? newValues = null, 
        string? ipAddress = null, string? userAgent = null, string? additionalData = null)
    {
        var auditLog = AuditLog.Create(action, entityType, entityId, userId, userType, 
            userEmail, oldValues, newValues, ipAddress, userAgent, additionalData);

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLogResponse>> SearchAuditLogsAsync(AuditLogSearchRequest request)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(request.Action))
            query = query.Where(al => al.Action.Contains(request.Action));

        if (!string.IsNullOrEmpty(request.EntityType))
            query = query.Where(al => al.EntityType == request.EntityType);

        if (request.EntityId.HasValue)
            query = query.Where(al => al.EntityId == request.EntityId);

        if (!string.IsNullOrEmpty(request.UserEmail))
            query = query.Where(al => al.UserEmail != null && al.UserEmail.Contains(request.UserEmail));

        if (!string.IsNullOrEmpty(request.UserType))
            query = query.Where(al => al.UserType == request.UserType);

        if (request.StartDate.HasValue)
            query = query.Where(al => al.CreatedAt >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(al => al.CreatedAt <= request.EndDate.Value);

        if (request.SecurityEventsOnly == true)
        {
            var securityActions = new[] { "LOGIN_FAILED", "ACCOUNT_SUSPENDED", "ACCOUNT_BANNED", "PASSWORD_CHANGED", "PERMISSION_DENIED", "SUSPICIOUS_ACTIVITY" };
            query = query.Where(al => securityActions.Contains(al.Action));
        }

        var logs = await query
            .OrderByDescending(al => al.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return logs.Select(MapToAuditLogResponse);
    }

    public async Task<IEnumerable<AuditLogResponse>> GetSecurityEventsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var securityActions = new[] { "LOGIN_FAILED", "ACCOUNT_SUSPENDED", "ACCOUNT_BANNED", "PASSWORD_CHANGED", "PERMISSION_DENIED", "SUSPICIOUS_ACTIVITY" };
        
        var query = _context.AuditLogs
            .Where(al => securityActions.Contains(al.Action));

        if (startDate.HasValue)
            query = query.Where(al => al.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(al => al.CreatedAt <= endDate.Value);

        var logs = await query
            .OrderByDescending(al => al.CreatedAt)
            .Take(100)
            .ToListAsync();

        return logs.Select(MapToAuditLogResponse);
    }

    // Helper methods
    private static AdminResponse MapToAdminResponse(Admin admin)
    {
        return new AdminResponse
        {
            Id = admin.Id,
            Email = admin.Email.Value,
            FirstName = admin.FirstName,
            LastName = admin.LastName,
            FullName = admin.GetFullName(),
            Role = admin.Role,
            IsActive = admin.IsActive,
            CreatedAt = admin.CreatedAt,
            UpdatedAt = admin.UpdatedAt
        };
    }

    private static BusinessApplicationResponse MapToBusinessApplicationResponse(BusinessApplication application)
    {
        return new BusinessApplicationResponse
        {
            Id = application.Id,
            BusinessName = application.BusinessName,
            Email = application.Email.Value,
            Phone = application.Phone,
            Address = application.Address,
            RequestedTier = application.RequestedTier,
            Status = application.Status,
            BusinessDescription = application.BusinessDescription,
            BusinessCategory = application.BusinessCategory,
            Website = application.Website,
            SubmittedAt = application.SubmittedAt,
            ReviewedAt = application.ReviewedAt,
            ReviewerName = application.Reviewer?.GetFullName(),
            ReviewNotes = application.ReviewNotes,
            RejectionReason = application.RejectionReason
        };
    }

    private static DisputeResponse MapToDisputeResponse(DisputeResolution dispute)
    {
        return new DisputeResponse
        {
            Id = dispute.Id,
            Title = dispute.Title,
            Description = dispute.Description,
            Type = dispute.Type,
            Status = dispute.Status,
            Priority = dispute.Priority,
            ReporterEmail = dispute.ReporterEmail,
            ReporterType = dispute.ReporterType,
            SubjectType = dispute.SubjectType,
            AssignedToName = dispute.AssignedAdmin?.GetFullName(),
            CreatedAt = dispute.CreatedAt,
            AssignedAt = dispute.AssignedAt,
            ResolvedAt = dispute.ResolvedAt,
            Resolution = dispute.Resolution,
            MessageCount = dispute.Messages.Count
        };
    }

    private static AuditLogResponse MapToAuditLogResponse(AuditLog log)
    {
        return new AuditLogResponse
        {
            Id = log.Id,
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            UserEmail = log.UserEmail,
            UserType = log.UserType,
            CreatedAt = log.CreatedAt,
            IpAddress = log.IpAddress,
            IsSecurityEvent = log.IsSecurityEvent(),
            AdditionalData = log.AdditionalData
        };
    }
}