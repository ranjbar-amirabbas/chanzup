namespace Chanzup.Domain.Entities;

public class SuspiciousActivity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SuspiciousActivitySeverity Severity { get; set; } = SuspiciousActivitySeverity.Low;
    public SuspiciousActivityStatus Status { get; set; } = SuspiciousActivityStatus.Open;
    public Guid? UserId { get; set; }
    public string? UserType { get; set; } // Player, Business, Staff
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Location { get; set; }
    public string ActivityData { get; set; } = string.Empty; // JSON data
    public decimal RiskScore { get; set; } = 0;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }
    public string? ActionTaken { get; set; }

    // Navigation properties
    public Admin? ReviewedByAdmin { get; set; }

    // Domain methods
    public bool RequiresImmedateAction()
    {
        return Severity == SuspiciousActivitySeverity.Critical && Status == SuspiciousActivityStatus.Open;
    }

    public bool CanBeReviewed()
    {
        return Status == SuspiciousActivityStatus.Open;
    }

    public void MarkAsReviewed(Guid reviewerId, string notes, string? actionTaken = null)
    {
        if (!CanBeReviewed())
            throw new InvalidOperationException("Activity cannot be reviewed in current status");

        Status = SuspiciousActivityStatus.Reviewed;
        ReviewedAt = DateTime.UtcNow;
        ReviewedBy = reviewerId;
        ReviewNotes = notes;
        ActionTaken = actionTaken;
    }

    public void MarkAsFalsePositive(Guid reviewerId, string notes)
    {
        if (!CanBeReviewed())
            throw new InvalidOperationException("Activity cannot be reviewed in current status");

        Status = SuspiciousActivityStatus.FalsePositive;
        ReviewedAt = DateTime.UtcNow;
        ReviewedBy = reviewerId;
        ReviewNotes = notes;
    }

    public void Escalate(SuspiciousActivitySeverity newSeverity)
    {
        if (Status != SuspiciousActivityStatus.Open)
            throw new InvalidOperationException("Cannot escalate reviewed activities");

        Severity = newSeverity;
    }

    public static SuspiciousActivity Create(
        string activityType,
        string description,
        SuspiciousActivitySeverity severity,
        Guid? userId,
        string? userType,
        string? userEmail,
        string activityData,
        decimal riskScore,
        string? ipAddress = null,
        string? userAgent = null,
        string? location = null)
    {
        return new SuspiciousActivity
        {
            ActivityType = activityType,
            Description = description,
            Severity = severity,
            UserId = userId,
            UserType = userType,
            UserEmail = userEmail,
            ActivityData = activityData,
            RiskScore = riskScore,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Location = location
        };
    }
}

public enum SuspiciousActivitySeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum SuspiciousActivityStatus
{
    Open = 0,
    Reviewed = 1,
    FalsePositive = 2
}