using OpsDesk.Application.Enums;

namespace OpsDesk.Application.Entities;

public sealed class Ticket
{
    private readonly List<TicketMessage> _messages = new();
    private readonly List<TicketNote> _notes = new();

    public string Id { get; private set; } = default!;
    public DateTime CreatedDate { get; private set; }
    public Priority Priority { get; private set; }
    public string RequesterName { get; private set; } = default!;
    public string RequesterEmail { get; private set; } = default!;
    public string Department { get; private set; } = default!;
    public ServiceStream ServiceStream { get; private set; }
    public DeploymentEnvironment Environment { get; private set; }
    public string Category { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string? TechnicalContext { get; private set; }
    public TicketStatus Status { get; private set; }
    public string? AssignedSpecialist { get; private set; }
    public string? ReturnReason { get; private set; }
    public string? ReturnedBy { get; private set; }
    public DateTime? ReturnedAt { get; private set; }
    public int TargetTatDays { get; private set; }
    public DateTime TatDueDate { get; private set; }
    public DateTime? CompletedDate { get; private set; }
    public string? ClosedBy { get; private set; }
    public TechnicalReview? Review { get; private set; }
    public Escalation? Escalation { get; private set; }
    public IReadOnlyList<TicketMessage> Messages => _messages;
    public IReadOnlyList<TicketNote> Notes => _notes;

    private Ticket() { }

    public static Ticket Create(
        string id,
        string requesterName,
        string requesterEmail,
        string department,
        Priority priority,
        ServiceStream serviceStream,
        DeploymentEnvironment environment,
        string category,
        string title,
        string description,
        int targetTatDays,
        DateTime tatDueDate,
        string? technicalContext = null)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Ticket ID is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(requesterName)) throw new ArgumentException("Requester name is required.", nameof(requesterName));
        if (string.IsNullOrWhiteSpace(requesterEmail)) throw new ArgumentException("Requester email is required.", nameof(requesterEmail));
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("Category is required.", nameof(category));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required.", nameof(description));

        return new Ticket
        {
            Id = id,
            CreatedDate = DateTime.UtcNow,
            RequesterName = requesterName,
            RequesterEmail = requesterEmail,
            Department = department,
            Priority = priority,
            ServiceStream = serviceStream,
            Environment = environment,
            Category = category,
            Title = title,
            Description = description,
            TechnicalContext = technicalContext,
            Status = TicketStatus.Open,
            TargetTatDays = targetTatDays,
            TatDueDate = tatDueDate
        };
    }

    public void Hydrate(
        DateTime createdDate,
        TicketStatus status,
        string? assignedSpecialist,
        string? returnReason,
        string? returnedBy,
        DateTime? returnedAt,
        DateTime tatDueDate,
        int targetTatDays,
        DateTime? completedDate,
        string? closedBy,
        TechnicalReview? review,
        Escalation? escalation,
        IEnumerable<TicketMessage>? messages,
        IEnumerable<TicketNote>? notes)
    {
        CreatedDate = createdDate;
        Status = status;
        AssignedSpecialist = assignedSpecialist;
        ReturnReason = returnReason;
        ReturnedBy = returnedBy;
        ReturnedAt = returnedAt;
        TatDueDate = tatDueDate;
        TargetTatDays = targetTatDays;
        CompletedDate = completedDate;
        ClosedBy = closedBy;
        Review = review;
        Escalation = escalation;
        if (messages is not null) _messages.AddRange(messages);
        if (notes is not null) _notes.AddRange(notes);
    }

    public void AssignSpecialist(string specialistName)
    {
        if (string.IsNullOrWhiteSpace(specialistName))
            throw new ArgumentException("Specialist name is required.", nameof(specialistName));
        if (Status is TicketStatus.Resolved)
            throw new InvalidOperationException("Cannot assign a resolved ticket.");

        AssignedSpecialist = specialistName;
        if (Status is TicketStatus.Open or TicketStatus.ReturnedToRequester)
            Status = TicketStatus.InProgress;
    }

    public void ReturnToRequester(string returnedBy, string reason)
    {
        if (Status is TicketStatus.Resolved)
            throw new InvalidOperationException("Cannot return a resolved ticket.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Return feedback reason is required.", nameof(reason));

        Status = TicketStatus.ReturnedToRequester;
        ReturnReason = reason.Trim();
        ReturnedBy = returnedBy;
        ReturnedAt = DateTime.UtcNow;
        _notes.Add(new TicketNote(returnedBy, $"Returned to requester: {ReturnReason}"));
    }

    public void Resubmit(string title, string description, string? technicalContext)
    {
        if (Status is not TicketStatus.ReturnedToRequester)
            throw new InvalidOperationException("Only tickets returned to the requester can be resubmitted.");
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required.", nameof(description));

        Title = title.Trim();
        Description = description.Trim();
        TechnicalContext = string.IsNullOrWhiteSpace(technicalContext) ? null : technicalContext.Trim();
        ReturnReason = null;
        ReturnedBy = null;
        ReturnedAt = null;
        Status = string.IsNullOrWhiteSpace(AssignedSpecialist) ? TicketStatus.Open : TicketStatus.InProgress;
    }

    public void SubmitForApproval(string submittedBy)
    {
        if (Status is not TicketStatus.InProgress)
            throw new InvalidOperationException("Only InProgress tickets can be submitted for leader approval.");
        Status = TicketStatus.PendingApproval;
        _notes.Add(new TicketNote(submittedBy, "Submitted technical analysis for Operations Lead approval."));
    }

    public void ReturnToProgress(string returnedBy, string feedback)
    {
        if (Status is not TicketStatus.PendingApproval)
            throw new InvalidOperationException("Only PendingApproval tickets can be returned to progress.");
        if (string.IsNullOrWhiteSpace(feedback))
            throw new ArgumentException("Feedback is required when returning a ticket to progress.", nameof(feedback));

        Status = TicketStatus.InProgress;
        ReturnReason = feedback.Trim();
        ReturnedBy = returnedBy;
        ReturnedAt = DateTime.UtcNow;
        _notes.Add(new TicketNote(returnedBy, $"Returned to specialist: {ReturnReason}"));
    }

    public void ApproveAndClose(string closedBy, string? resolutionSummary = null)
    {
        if (Status is TicketStatus.Resolved)
            throw new InvalidOperationException("Ticket is already resolved.");

        if (!string.IsNullOrWhiteSpace(resolutionSummary))
        {
            Review ??= new TechnicalReview();
            Review.Set(
                Review.ArchitectureReference,
                Review.TechnicalAnalysis,
                Review.OperationalRisks,
                resolutionSummary.Trim(),
                closedBy);
        }

        Status = TicketStatus.Resolved;
        CompletedDate = DateTime.UtcNow;
        ClosedBy = closedBy;
        ReturnReason = null;
    }

    public void UpdateStatus(TicketStatus newStatus)
    {
        Status = newStatus;
        CompletedDate = newStatus is TicketStatus.Resolved ? DateTime.UtcNow : null;
    }

    public void AddMessage(string senderName, string senderRole, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Message cannot be empty.", nameof(text));
        _messages.Add(new TicketMessage(senderName, senderRole, text));
    }

    public void AddInternalNote(string author, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Note cannot be empty.", nameof(text));
        _notes.Add(new TicketNote(author, text));
    }

    public void SetTechnicalReview(
        string? architectureReference,
        string? technicalAnalysis,
        string? operationalRisks,
        string? resolutionSummary,
        string? reviewedBy)
    {
        Review ??= new TechnicalReview();
        Review.Set(architectureReference, technicalAnalysis, operationalRisks, resolutionSummary, reviewedBy);
    }

    public void EscalateToTeam(string team, string? reason)
    {
        if (Status is TicketStatus.Resolved)
            throw new InvalidOperationException("Cannot escalate a resolved ticket.");
        if (string.IsNullOrWhiteSpace(team))
            throw new ArgumentException("Escalation target team is required.", nameof(team));

        Escalation = new Escalation(team, reason);
    }

    public void ChangePriority(Priority newPriority, int targetTatDays, DateTime tatDueDate)
    {
        Priority = newPriority;
        TargetTatDays = targetTatDays;
        TatDueDate = tatDueDate;
    }
}

public sealed class TicketMessage
{
    public long Id { get; private set; }
    public string SenderName { get; private set; } = default!;
    public string SenderRole { get; private set; } = default!;
    public string MessageText { get; private set; } = default!;
    public DateTime CreatedDate { get; private set; }
    public bool IsNew { get; private set; } = true;

    public TicketMessage(string senderName, string senderRole, string messageText)
    {
        SenderName = senderName;
        SenderRole = senderRole;
        MessageText = messageText;
        CreatedDate = DateTime.UtcNow;
    }

    public TicketMessage(long id, string senderName, string senderRole, string messageText, DateTime createdDate)
    {
        Id = id;
        SenderName = senderName;
        SenderRole = senderRole;
        MessageText = messageText;
        CreatedDate = createdDate;
        IsNew = false;
    }

    public void MarkPersisted(long id)
    {
        Id = id;
        IsNew = false;
    }
}

public sealed class TicketNote
{
    public long Id { get; private set; }
    public string Author { get; private set; } = default!;
    public string NoteText { get; private set; } = default!;
    public DateTime CreatedDate { get; private set; }
    public bool IsNew { get; private set; } = true;

    public TicketNote(string author, string noteText)
    {
        Author = author;
        NoteText = noteText;
        CreatedDate = DateTime.UtcNow;
    }

    public TicketNote(long id, string author, string noteText, DateTime createdDate)
    {
        Id = id;
        Author = author;
        NoteText = noteText;
        CreatedDate = createdDate;
        IsNew = false;
    }

    public void MarkPersisted(long id)
    {
        Id = id;
        IsNew = false;
    }
}

public sealed class TechnicalReview
{
    public string? ArchitectureReference { get; private set; }
    public string? TechnicalAnalysis { get; private set; }
    public string? OperationalRisks { get; private set; }
    public string? ResolutionSummary { get; private set; }
    public string? ReviewedBy { get; private set; }
    public DateTime UpdatedDate { get; private set; }

    public void Set(
        string? architectureReference,
        string? technicalAnalysis,
        string? operationalRisks,
        string? resolutionSummary,
        string? reviewedBy)
    {
        ArchitectureReference = architectureReference;
        TechnicalAnalysis = technicalAnalysis;
        OperationalRisks = operationalRisks;
        ResolutionSummary = resolutionSummary;
        ReviewedBy = reviewedBy;
        UpdatedDate = DateTime.UtcNow;
    }
}

public sealed class Escalation
{
    public string EscalatedTeam { get; private set; } = default!;
    public string? EscalationReason { get; private set; }
    public DateTime EscalatedDate { get; private set; }

    public Escalation(string escalatedTeam, string? escalationReason)
    {
        EscalatedTeam = escalatedTeam;
        EscalationReason = escalationReason;
        EscalatedDate = DateTime.UtcNow;
    }
}
