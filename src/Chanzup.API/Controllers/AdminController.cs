using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Chanzup.Application.DTOs;
using Chanzup.Application.Interfaces;
using Chanzup.API.Authorization;
using System.Security.Claims;

namespace Chanzup.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminManagementService _adminService;
    private readonly IFraudDetectionService _fraudService;
    private readonly ISystemParameterService _parameterService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IAdminManagementService adminService, 
        IFraudDetectionService fraudService,
        ISystemParameterService parameterService,
        ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _fraudService = fraudService;
        _parameterService = parameterService;
        _logger = logger;
    }

    // Admin Management
    [HttpPost("admins")]
    [RequirePermission("admin:write")]
    public async Task<ActionResult<AdminResponse>> CreateAdmin([FromBody] RegisterAdminRequest request)
    {
        try
        {
            var currentAdminId = GetCurrentUserId();
            var admin = await _adminService.CreateAdminAsync(request, currentAdminId);
            
            _logger.LogInformation("Admin {AdminId} created new admin {NewAdminId}", currentAdminId, admin.Id);
            
            return CreatedAtAction(nameof(GetAdmin), new { id = admin.Id }, admin);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating admin");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("admins")]
    [RequirePermission("admin:read")]
    public async Task<ActionResult<IEnumerable<AdminResponse>>> GetAllAdmins()
    {
        try
        {
            var admins = await _adminService.GetAllAdminsAsync();
            return Ok(admins);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving admins");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("admins/{id}")]
    [RequirePermission("admin:read")]
    public async Task<ActionResult<AdminResponse>> GetAdmin(Guid id)
    {
        try
        {
            var admin = await _adminService.GetAdminAsync(id);
            return Ok(admin);
        }
        catch (ArgumentException)
        {
            return NotFound(new { error = "Admin not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving admin {AdminId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPut("admins/{id}")]
    [RequirePermission("admin:write")]
    public async Task<ActionResult<AdminResponse>> UpdateAdmin(Guid id, [FromBody] UpdateAdminRequest request)
    {
        try
        {
            var admin = await _adminService.UpdateAdminAsync(id, request);
            
            _logger.LogInformation("Admin {AdminId} updated admin {UpdatedAdminId}", GetCurrentUserId(), id);
            
            return Ok(admin);
        }
        catch (ArgumentException)
        {
            return NotFound(new { error = "Admin not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating admin {AdminId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpDelete("admins/{id}")]
    [RequirePermission("admin:write")]
    public async Task<IActionResult> DeleteAdmin(Guid id)
    {
        try
        {
            await _adminService.DeleteAdminAsync(id);
            
            _logger.LogInformation("Admin {AdminId} deleted admin {DeletedAdminId}", GetCurrentUserId(), id);
            
            return NoContent();
        }
        catch (ArgumentException)
        {
            return NotFound(new { error = "Admin not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting admin {AdminId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // Business Application Management
    [HttpGet("applications")]
    [RequirePermission("business:read")]
    public async Task<ActionResult<IEnumerable<BusinessApplicationResponse>>> GetPendingApplications()
    {
        try
        {
            var applications = await _adminService.GetPendingApplicationsAsync();
            return Ok(applications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending applications");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("applications/{id}")]
    [RequirePermission("business:read")]
    public async Task<ActionResult<BusinessApplicationResponse>> GetApplication(Guid id)
    {
        try
        {
            var application = await _adminService.GetApplicationAsync(id);
            return Ok(application);
        }
        catch (ArgumentException)
        {
            return NotFound(new { error = "Application not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application {ApplicationId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("applications/{id}/review")]
    [RequirePermission("business:write")]
    public async Task<ActionResult<BusinessApplicationResponse>> ReviewApplication(Guid id, [FromBody] ReviewBusinessApplicationRequest request)
    {
        try
        {
            var reviewerId = GetCurrentUserId();
            var application = await _adminService.ReviewApplicationAsync(id, request, reviewerId);
            
            _logger.LogInformation("Admin {AdminId} reviewed application {ApplicationId} with status {Status}", 
                reviewerId, id, request.Status);
            
            return Ok(application);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviewing application {ApplicationId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("applications/{id}/approve")]
    [RequirePermission("business:write")]
    public async Task<ActionResult> ApproveApplication(Guid id)
    {
        try
        {
            var reviewerId = GetCurrentUserId();
            var business = await _adminService.ApproveApplicationAsync(id, reviewerId);
            
            _logger.LogInformation("Admin {AdminId} approved application {ApplicationId}, created business {BusinessId}", 
                reviewerId, id, business.Id);
            
            return Ok(new { businessId = business.Id, message = "Application approved and business created" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving application {ApplicationId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // Account Management
    [HttpPost("businesses/{id}/suspend")]
    [RequirePermission("business:write")]
    public async Task<ActionResult<AccountSuspensionResponse>> SuspendBusiness(Guid id, [FromBody] SuspendAccountRequest request)
    {
        try
        {
            var suspendedBy = GetCurrentUserId();
            var result = await _adminService.SuspendBusinessAsync(id, request, suspendedBy);
            
            _logger.LogInformation("Admin {AdminId} suspended business {BusinessId}", suspendedBy, id);
            
            return Ok(result);
        }
        catch (ArgumentException)
        {
            return NotFound(new { error = "Business not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending business {BusinessId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("businesses/{id}/unsuspend")]
    [RequirePermission("business:write")]
    public async Task<ActionResult<AccountSuspensionResponse>> UnsuspendBusiness(Guid id)
    {
        try
        {
            var unsuspendedBy = GetCurrentUserId();
            var result = await _adminService.UnsuspendBusinessAsync(id, unsuspendedBy);
            
            _logger.LogInformation("Admin {AdminId} unsuspended business {BusinessId}", unsuspendedBy, id);
            
            return Ok(result);
        }
        catch (ArgumentException)
        {
            return NotFound(new { error = "Business not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsuspending business {BusinessId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("players/{id}/suspend")]
    [RequirePermission("user:write")]
    public async Task<ActionResult<AccountSuspensionResponse>> SuspendPlayer(Guid id, [FromBody] SuspendAccountRequest request)
    {
        try
        {
            var suspendedBy = GetCurrentUserId();
            var result = await _adminService.SuspendPlayerAsync(id, request, suspendedBy);
            
            _logger.LogInformation("Admin {AdminId} suspended player {PlayerId}", suspendedBy, id);
            
            return Ok(result);
        }
        catch (ArgumentException)
        {
            return NotFound(new { error = "Player not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending player {PlayerId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("players/{id}/unsuspend")]
    [RequirePermission("user:write")]
    public async Task<ActionResult<AccountSuspensionResponse>> UnsuspendPlayer(Guid id)
    {
        try
        {
            var unsuspendedBy = GetCurrentUserId();
            var result = await _adminService.UnsuspendPlayerAsync(id, unsuspendedBy);
            
            _logger.LogInformation("Admin {AdminId} unsuspended player {PlayerId}", unsuspendedBy, id);
            
            return Ok(result);
        }
        catch (ArgumentException)
        {
            return NotFound(new { error = "Player not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsuspending player {PlayerId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("accounts/{id}/ban")]
    [RequirePermission("user:delete")]
    public async Task<IActionResult> BanAccount(Guid id, [FromQuery] string accountType, [FromBody] SuspendAccountRequest request)
    {
        try
        {
            var bannedBy = GetCurrentUserId();
            await _adminService.BanAccountAsync(id, accountType, request.Reason, bannedBy);
            
            _logger.LogInformation("Admin {AdminId} banned {AccountType} {AccountId}", bannedBy, accountType, id);
            
            return Ok(new { message = $"{accountType} account banned successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error banning {AccountType} {AccountId}", accountType, id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // Dispute Resolution
    [HttpGet("disputes")]
    [RequirePermission("admin:read")]
    public async Task<ActionResult<IEnumerable<DisputeResponse>>> GetDisputes(
        [FromQuery] Domain.Entities.DisputeStatus? status = null,
        [FromQuery] Guid? assignedTo = null)
    {
        try
        {
            var disputes = await _adminService.GetDisputesAsync(status, assignedTo);
            return Ok(disputes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving disputes");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("disputes/{id}")]
    [RequirePermission("admin:read")]
    public async Task<ActionResult<DisputeResponse>> GetDispute(Guid id)
    {
        try
        {
            var dispute = await _adminService.GetDisputeAsync(id);
            return Ok(dispute);
        }
        catch (ArgumentException)
        {
            return NotFound(new { error = "Dispute not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dispute {DisputeId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("disputes/{id}/assign")]
    [RequirePermission("admin:write")]
    public async Task<ActionResult<DisputeResponse>> AssignDispute(Guid id, [FromBody] AssignDisputeRequest request)
    {
        try
        {
            var dispute = await _adminService.AssignDisputeAsync(id, request.AdminId);
            
            _logger.LogInformation("Dispute {DisputeId} assigned to admin {AdminId}", id, request.AdminId);
            
            return Ok(dispute);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning dispute {DisputeId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("disputes/{id}/resolve")]
    [RequirePermission("admin:write")]
    public async Task<ActionResult<DisputeResponse>> ResolveDispute(Guid id, [FromBody] ResolveDisputeRequest request)
    {
        try
        {
            var dispute = await _adminService.ResolveDisputeAsync(id, request);
            
            _logger.LogInformation("Admin {AdminId} resolved dispute {DisputeId}", GetCurrentUserId(), id);
            
            return Ok(dispute);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving dispute {DisputeId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("disputes/{id}/messages")]
    [RequirePermission("admin:write")]
    public async Task<IActionResult> AddDisputeMessage(Guid id, [FromBody] AddDisputeMessageRequest request)
    {
        try
        {
            var senderId = GetCurrentUserId();
            await _adminService.AddDisputeMessageAsync(id, request, senderId, "Admin");
            
            return Ok(new { message = "Message added successfully" });
        }
        catch (ArgumentException)
        {
            return NotFound(new { error = "Dispute not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message to dispute {DisputeId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // Audit Logs
    [HttpGet("audit-logs")]
    [RequirePermission("admin:read")]
    public async Task<ActionResult<IEnumerable<AuditLogResponse>>> SearchAuditLogs([FromQuery] AuditLogSearchRequest request)
    {
        try
        {
            var logs = await _adminService.SearchAuditLogsAsync(request);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching audit logs");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("security-events")]
    [RequirePermission("admin:read")]
    public async Task<ActionResult<IEnumerable<AuditLogResponse>>> GetSecurityEvents(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var events = await _adminService.GetSecurityEventsAsync(startDate, endDate);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security events");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }

    private string GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
            return Request.Headers["X-Forwarded-For"].ToString();
        else
            return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";
    }

    // Fraud Detection and Monitoring
    [HttpGet("suspicious-activities")]
    [RequirePermission("admin:read")]
    public async Task<ActionResult<IEnumerable<SuspiciousActivityResponse>>> GetSuspiciousActivities(
        [FromQuery] Domain.Entities.SuspiciousActivityStatus? status = null,
        [FromQuery] Domain.Entities.SuspiciousActivitySeverity? minSeverity = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var activities = await _fraudService.GetSuspiciousActivitiesAsync(status, minSeverity, startDate, endDate);
            var response = activities.Select(a => new SuspiciousActivityResponse
            {
                Id = a.Id,
                ActivityType = a.ActivityType,
                Description = a.Description,
                Severity = a.Severity,
                Status = a.Status,
                UserEmail = a.UserEmail,
                UserType = a.UserType,
                RiskScore = a.RiskScore,
                DetectedAt = a.DetectedAt,
                ReviewedAt = a.ReviewedAt,
                ReviewedByName = a.ReviewedByAdmin?.Email?.Value,
                ReviewNotes = a.ReviewNotes,
                ActionTaken = a.ActionTaken
            });
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving suspicious activities");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("suspicious-activities/{id}/review")]
    [RequirePermission("admin:write")]
    public async Task<ActionResult<SuspiciousActivityResponse>> ReviewSuspiciousActivity(Guid id, [FromBody] ReviewSuspiciousActivityRequest request)
    {
        try
        {
            var reviewerId = GetCurrentUserId();
            var activity = await _fraudService.ReviewSuspiciousActivityAsync(id, reviewerId, request.Notes, request.ActionTaken);
            
            var response = new SuspiciousActivityResponse
            {
                Id = activity.Id,
                ActivityType = activity.ActivityType,
                Description = activity.Description,
                Severity = activity.Severity,
                Status = activity.Status,
                UserEmail = activity.UserEmail,
                UserType = activity.UserType,
                RiskScore = activity.RiskScore,
                DetectedAt = activity.DetectedAt,
                ReviewedAt = activity.ReviewedAt,
                ReviewedByName = activity.ReviewedByAdmin?.Email?.Value,
                ReviewNotes = activity.ReviewNotes,
                ActionTaken = activity.ActionTaken
            };
            
            _logger.LogInformation("Admin {AdminId} reviewed suspicious activity {ActivityId}", reviewerId, id);
            
            return Ok(response);
        }
        catch (ArgumentException)
        {
            return NotFound(new { error = "Suspicious activity not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviewing suspicious activity {ActivityId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("suspicious-activities/{id}/false-positive")]
    [RequirePermission("admin:write")]
    public async Task<ActionResult<SuspiciousActivityResponse>> MarkAsFalsePositive(Guid id, [FromBody] ReviewSuspiciousActivityRequest request)
    {
        try
        {
            var reviewerId = GetCurrentUserId();
            var activity = await _fraudService.MarkAsFalsePositiveAsync(id, reviewerId, request.Notes);
            
            var response = new SuspiciousActivityResponse
            {
                Id = activity.Id,
                ActivityType = activity.ActivityType,
                Description = activity.Description,
                Severity = activity.Severity,
                Status = activity.Status,
                UserEmail = activity.UserEmail,
                UserType = activity.UserType,
                RiskScore = activity.RiskScore,
                DetectedAt = activity.DetectedAt,
                ReviewedAt = activity.ReviewedAt,
                ReviewedByName = activity.ReviewedByAdmin?.Email?.Value,
                ReviewNotes = activity.ReviewNotes,
                ActionTaken = activity.ActionTaken
            };
            
            _logger.LogInformation("Admin {AdminId} marked suspicious activity {ActivityId} as false positive", reviewerId, id);
            
            return Ok(response);
        }
        catch (ArgumentException)
        {
            return NotFound(new { error = "Suspicious activity not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking suspicious activity {ActivityId} as false positive", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // System Parameter Management
    [HttpGet("system-parameters")]
    [RequirePermission("admin:read")]
    public async Task<ActionResult<IEnumerable<SystemParameterResponse>>> GetSystemParameters()
    {
        try
        {
            var parameters = await _parameterService.GetAllParametersAsync();
            return Ok(parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system parameters");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPut("system-parameters/{key}")]
    [RequirePermission("admin:write")]
    public async Task<ActionResult<SystemParameterResponse>> UpdateSystemParameter(string key, [FromBody] UpdateSystemParameterRequest request)
    {
        try
        {
            var updatedBy = GetCurrentUserId();
            var parameter = await _parameterService.UpdateParameterAsync(key, request, updatedBy);
            
            _logger.LogInformation("Admin {AdminId} updated system parameter {Key} to {Value}", updatedBy, key, request.Value);
            
            return Ok(parameter);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating system parameter {Key}", key);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

public class AssignDisputeRequest
{
    public Guid AdminId { get; set; }
}

// Additional DTOs for fraud detection
public class SuspiciousActivityResponse
{
    public Guid Id { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Domain.Entities.SuspiciousActivitySeverity Severity { get; set; }
    public Domain.Entities.SuspiciousActivityStatus Status { get; set; }
    public string? UserEmail { get; set; }
    public string? UserType { get; set; }
    public decimal RiskScore { get; set; }
    public DateTime DetectedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedByName { get; set; }
    public string? ReviewNotes { get; set; }
    public string? ActionTaken { get; set; }
}

public class ReviewSuspiciousActivityRequest
{
    public string Notes { get; set; } = string.Empty;
    public string? ActionTaken { get; set; }
}

public class SystemParameterResponse
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public string? UpdatedByName { get; set; }
}