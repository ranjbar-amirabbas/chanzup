using Chanzup.Application.DTOs;
using Chanzup.Domain.Entities;

namespace Chanzup.Application.Interfaces;

public interface IAdminManagementService
{
    // Admin Management
    Task<AdminResponse> CreateAdminAsync(RegisterAdminRequest request, Guid createdBy);
    Task<AdminResponse> GetAdminAsync(Guid adminId);
    Task<IEnumerable<AdminResponse>> GetAllAdminsAsync();
    Task<AdminResponse> UpdateAdminAsync(Guid adminId, UpdateAdminRequest request);
    Task DeleteAdminAsync(Guid adminId);

    // Business Application Management
    Task<IEnumerable<BusinessApplicationResponse>> GetPendingApplicationsAsync();
    Task<BusinessApplicationResponse> GetApplicationAsync(Guid applicationId);
    Task<BusinessApplicationResponse> ReviewApplicationAsync(Guid applicationId, ReviewBusinessApplicationRequest request, Guid reviewerId);
    Task<Business> ApproveApplicationAsync(Guid applicationId, Guid reviewerId);

    // Account Management
    Task<AccountSuspensionResponse> SuspendBusinessAsync(Guid businessId, SuspendAccountRequest request, Guid suspendedBy);
    Task<AccountSuspensionResponse> SuspendPlayerAsync(Guid playerId, SuspendAccountRequest request, Guid suspendedBy);
    Task<AccountSuspensionResponse> UnsuspendBusinessAsync(Guid businessId, Guid unsuspendedBy);
    Task<AccountSuspensionResponse> UnsuspendPlayerAsync(Guid playerId, Guid unsuspendedBy);
    Task BanAccountAsync(Guid accountId, string accountType, string reason, Guid bannedBy);

    // Dispute Resolution
    Task<DisputeResponse> CreateDisputeAsync(CreateDisputeRequest request, Guid reporterId, string reporterType, string reporterEmail);
    Task<IEnumerable<DisputeResponse>> GetDisputesAsync(DisputeStatus? status = null, Guid? assignedTo = null);
    Task<DisputeResponse> GetDisputeAsync(Guid disputeId);
    Task<DisputeResponse> AssignDisputeAsync(Guid disputeId, Guid adminId);
    Task<DisputeResponse> ResolveDisputeAsync(Guid disputeId, ResolveDisputeRequest request);
    Task AddDisputeMessageAsync(Guid disputeId, AddDisputeMessageRequest request, Guid senderId, string senderType);

    // Audit Logging
    Task LogActionAsync(string action, string entityType, Guid? entityId, Guid? userId, string userType, 
        string? userEmail = null, string? oldValues = null, string? newValues = null, 
        string? ipAddress = null, string? userAgent = null, string? additionalData = null);
    Task<IEnumerable<AuditLogResponse>> SearchAuditLogsAsync(AuditLogSearchRequest request);
    Task<IEnumerable<AuditLogResponse>> GetSecurityEventsAsync(DateTime? startDate = null, DateTime? endDate = null);
}