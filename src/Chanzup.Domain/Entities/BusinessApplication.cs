using Chanzup.Domain.ValueObjects;

namespace Chanzup.Domain.Entities;

public class BusinessApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BusinessName { get; set; } = string.Empty;
    public Email Email { get; set; } = new("placeholder@example.com");
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public Location? Location { get; set; }
    public SubscriptionTier RequestedTier { get; set; } = SubscriptionTier.Basic;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    public string? BusinessDescription { get; set; }
    public string? BusinessCategory { get; set; }
    public string? Website { get; set; }
    public string? SocialMediaLinks { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? BusinessId { get; set; } // Set when approved

    // Navigation properties
    public Admin? Reviewer { get; set; }
    public Business? Business { get; set; }

    // Domain methods
    public bool CanBeReviewed()
    {
        return Status == ApplicationStatus.Pending;
    }

    public void Approve(Guid reviewerId, string? notes = null)
    {
        if (!CanBeReviewed())
            throw new InvalidOperationException("Application cannot be reviewed in current status");

        Status = ApplicationStatus.Approved;
        ReviewedAt = DateTime.UtcNow;
        ReviewedBy = reviewerId;
        ReviewNotes = notes;
    }

    public void Reject(Guid reviewerId, string rejectionReason, string? notes = null)
    {
        if (!CanBeReviewed())
            throw new InvalidOperationException("Application cannot be reviewed in current status");

        Status = ApplicationStatus.Rejected;
        ReviewedAt = DateTime.UtcNow;
        ReviewedBy = reviewerId;
        RejectionReason = rejectionReason;
        ReviewNotes = notes;
    }

    public void RequestMoreInfo(Guid reviewerId, string requestDetails)
    {
        if (!CanBeReviewed())
            throw new InvalidOperationException("Application cannot be reviewed in current status");

        Status = ApplicationStatus.MoreInfoRequired;
        ReviewedAt = DateTime.UtcNow;
        ReviewedBy = reviewerId;
        ReviewNotes = requestDetails;
    }

    public void Resubmit()
    {
        if (Status != ApplicationStatus.MoreInfoRequired)
            throw new InvalidOperationException("Application can only be resubmitted when more info is required");

        Status = ApplicationStatus.Pending;
        ReviewedAt = null;
        ReviewedBy = null;
        ReviewNotes = null;
    }
}

public enum ApplicationStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    MoreInfoRequired = 3
}