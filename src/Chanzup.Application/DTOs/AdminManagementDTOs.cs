using System.ComponentModel.DataAnnotations;
using Chanzup.Domain.Entities;

namespace Chanzup.Application.DTOs;

// Admin Registration and Management
public class RegisterAdminRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public AdminRole Role { get; set; } = AdminRole.Moderator;
}

public class AdminResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public AdminRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedByName { get; set; }
}

public class UpdateAdminRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public AdminRole? Role { get; set; }
    public bool? IsActive { get; set; }
}

// Business Application Management
public class BusinessApplicationResponse
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public SubscriptionTier RequestedTier { get; set; }
    public ApplicationStatus Status { get; set; }
    public string? BusinessDescription { get; set; }
    public string? BusinessCategory { get; set; }
    public string? Website { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewerName { get; set; }
    public string? ReviewNotes { get; set; }
    public string? RejectionReason { get; set; }
}

public class ReviewBusinessApplicationRequest
{
    [Required]
    public ApplicationStatus Status { get; set; }

    public string? ReviewNotes { get; set; }

    public string? RejectionReason { get; set; }
}

// Account Management
public class SuspendAccountRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;

    public DateTime? SuspensionEndDate { get; set; }

    public bool IsPermanent { get; set; } = false;
}

public class AccountSuspensionResponse
{
    public Guid AccountId { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsSuspended { get; set; }
    public string? SuspensionReason { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public DateTime? SuspensionEndDate { get; set; }
    public string? SuspendedByName { get; set; }
}

// Dispute Resolution
public class CreateDisputeRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DisputeType Type { get; set; }

    public DisputePriority Priority { get; set; } = DisputePriority.Medium;

    public Guid? SubjectId { get; set; }

    public string? SubjectType { get; set; }
}

public class DisputeResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DisputeType Type { get; set; }
    public DisputeStatus Status { get; set; }
    public DisputePriority Priority { get; set; }
    public string? ReporterEmail { get; set; }
    public string ReporterType { get; set; } = string.Empty;
    public string? SubjectType { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Resolution { get; set; }
    public int MessageCount { get; set; }
}

public class ResolveDisputeRequest
{
    [Required]
    public string Resolution { get; set; } = string.Empty;

    public string? AdminNotes { get; set; }
}

public class AddDisputeMessageRequest
{
    [Required]
    public string Content { get; set; } = string.Empty;

    public bool IsInternal { get; set; } = false;
}

// Audit Logs
public class AuditLogResponse
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? UserEmail { get; set; }
    public string UserType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? IpAddress { get; set; }
    public bool IsSecurityEvent { get; set; }
    public string? AdditionalData { get; set; }
}

public class AuditLogSearchRequest
{
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? SecurityEventsOnly { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

// System Parameters
public class SystemParameterResponse
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedByName { get; set; }
}

public class UpdateSystemParameterRequest
{
    [Required]
    public string Value { get; set; } = string.Empty;
}