namespace Chanzup.Domain.Entities;

public class DisputeResolution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DisputeType Type { get; set; } = DisputeType.General;
    public DisputeStatus Status { get; set; } = DisputeStatus.Open;
    public DisputePriority Priority { get; set; } = DisputePriority.Medium;
    public Guid? ReporterId { get; set; }
    public string ReporterType { get; set; } = string.Empty; // Player, Business, Staff
    public string? ReporterEmail { get; set; }
    public Guid? SubjectId { get; set; } // ID of the entity being disputed
    public string? SubjectType { get; set; } // Business, Player, Campaign, etc.
    public Guid? AssignedTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AssignedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Resolution { get; set; }
    public string? AdminNotes { get; set; }

    // Navigation properties
    public Admin? AssignedAdmin { get; set; }
    public ICollection<DisputeMessage> Messages { get; set; } = new List<DisputeMessage>();

    // Domain methods
    public bool CanBeAssigned()
    {
        return Status == DisputeStatus.Open && AssignedTo == null;
    }

    public bool CanBeResolved()
    {
        return Status == DisputeStatus.InProgress || Status == DisputeStatus.Open;
    }

    public void AssignTo(Guid adminId)
    {
        if (!CanBeAssigned() && AssignedTo != adminId)
            throw new InvalidOperationException("Dispute cannot be assigned in current state");

        AssignedTo = adminId;
        AssignedAt = DateTime.UtcNow;
        Status = DisputeStatus.InProgress;
    }

    public void Resolve(string resolution, string? adminNotes = null)
    {
        if (!CanBeResolved())
            throw new InvalidOperationException("Dispute cannot be resolved in current state");

        Status = DisputeStatus.Resolved;
        Resolution = resolution;
        AdminNotes = adminNotes;
        ResolvedAt = DateTime.UtcNow;
    }

    public void Close(string? adminNotes = null)
    {
        Status = DisputeStatus.Closed;
        AdminNotes = adminNotes;
        ResolvedAt = DateTime.UtcNow;
    }

    public void Escalate(DisputePriority newPriority)
    {
        if (Status == DisputeStatus.Resolved || Status == DisputeStatus.Closed)
            throw new InvalidOperationException("Cannot escalate resolved or closed disputes");

        Priority = newPriority;
    }

    public void AddMessage(string content, Guid senderId, string senderType)
    {
        Messages.Add(new DisputeMessage
        {
            DisputeId = Id,
            Content = content,
            SenderId = senderId,
            SenderType = senderType
        });
    }
}

public class DisputeMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DisputeId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid SenderId { get; set; }
    public string SenderType { get; set; } = string.Empty; // Admin, Player, Business
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsInternal { get; set; } = false; // Internal admin notes

    // Navigation properties
    public DisputeResolution Dispute { get; set; } = null!;
}

public enum DisputeType
{
    General = 0,
    PrizeRedemption = 1,
    AccountSuspension = 2,
    FraudReport = 3,
    TechnicalIssue = 4,
    BusinessComplaint = 5,
    PlayerComplaint = 6
}

public enum DisputeStatus
{
    Open = 0,
    InProgress = 1,
    Resolved = 2,
    Closed = 3
}

public enum DisputePriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}