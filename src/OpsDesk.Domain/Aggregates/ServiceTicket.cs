using OpsDesk.Domain.Entities;
using OpsDesk.Domain.Enums;
using OpsDesk.Domain.Events;
using OpsDesk.Domain.ValueObjects;

namespace OpsDesk.Domain.Aggregates;

public sealed class ServiceTicket
{
    private readonly List<TicketMessage> _messages = new();
    private readonly List<TicketNote> _notes = new();
    private readonly List<DomainEvent> _domainEvents = new();

    public TicketId Id { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public Priority Priority { get; private set; }
    public RequesterInfo Requester { get; private set; }
    public ServiceStream ServiceStream { get; private set; }
    public DeploymentEnvironment Environment { get; private set; }
    public string Category { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string? TechnicalContext { get; private set; }
    public TicketStatus Status { get; private set; }
    public string? AssignedSpecialist { get; private set; }
    public SlaWindow Sla { get; private set; }
    public DateTime? CompletedDate { get; private set; }
    public TechnicalReview? Review { get; private set; }
    public Escalation? Escalation { get; private set; }
    public IReadOnlyList<TicketMessage> Messages => _messages;
    public IReadOnlyList<TicketNote> Notes => _notes;
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents;

    private ServiceTicket()
    {
        Requester = default!;
        Category = default!;
        Title = default!;
        Description = default!;
        Sla = default!;
        Id = default!;
    }

    public static ServiceTicket Create(
        TicketId id,
        RequesterInfo requester,
        Priority priority,
        ServiceStream serviceStream,
        DeploymentEnvironment environment,
        string category,
        string title,
        string description,
        string? technicalContext = null)
    {
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("Category is required.", nameof(category));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required.", nameof(description));

        var aggregate = new ServiceTicket
        {
            Id = id,
            CreatedDate = DateTime.UtcNow,
            Requester = requester,
            Priority = priority,
            ServiceStream = serviceStream,
            Environment = environment,
            Category = category,
            Title = title,
            Description = description,
            TechnicalContext = technicalContext,
            Status = TicketStatus.Open,
            Sla = SlaWindow.Create(priority)
        };

        aggregate.Raise(new TicketSubmittedEvent(id, requester.Email, serviceStream, category, priority));
        return aggregate;
    }

    public void Hydrate(
        DateTime createdDate,
        TicketStatus status,
        string? assignedSpecialist,
        DateTime tatDueDate,
        int targetTatDays,
        DateTime? completedDate,
        TechnicalReview? review,
        Escalation? escalation,
        IEnumerable<TicketMessage>? messages,
        IEnumerable<TicketNote>? notes)
    {
        CreatedDate = createdDate;
        Status = status;
        AssignedSpecialist = assignedSpecialist;
        Sla = SlaWindow.FromStored(targetTatDays, tatDueDate);
        CompletedDate = completedDate;
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
        if (Status is TicketStatus.Open)
            Status = TicketStatus.InProgress;

        Raise(new TicketAssignedEvent(Id, specialistName, Sla.TatDueDate));
    }

    public void UpdateStatus(TicketStatus newStatus)
    {
        var old = Status;
        Status = newStatus;
        CompletedDate = newStatus is TicketStatus.Resolved ? DateTime.UtcNow : null;

        Raise(new TicketStatusChangedEvent(Id, old, newStatus));
    }

    public void AddMessage(string senderName, string senderRole, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Message cannot be empty.", nameof(text));
        _messages.Add(new TicketMessage(senderName, senderRole, text));
    }

    public void AddInternalNote(string author, string text, bool isOperationsStaff)
    {
        if (!isOperationsStaff)
            throw new InvalidOperationException("Only operations staff can add internal engineering notes.");
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
        Raise(new TicketEscalatedEvent(Id, team, reason));
    }

    public void ChangePriority(Priority newPriority)
    {
        Priority = newPriority;
        Sla = SlaWindow.Create(newPriority);
    }

    private void Raise(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
