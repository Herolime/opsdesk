using OpsDesk.Domain.Enums;
using OpsDesk.Domain.ValueObjects;

namespace OpsDesk.Domain.Events;

public abstract record DomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed record TicketSubmittedEvent(
    TicketId TicketId,
    string RequesterEmail,
    ServiceStream ServiceStream,
    string Category,
    Priority Priority) : DomainEvent;

public sealed record TicketAssignedEvent(
    TicketId TicketId,
    string SpecialistName,
    DateTime TatDueDate) : DomainEvent;

public sealed record TicketStatusChangedEvent(
    TicketId TicketId,
    TicketStatus OldStatus,
    TicketStatus NewStatus) : DomainEvent;

public sealed record TicketEscalatedEvent(
    TicketId TicketId,
    string EscalatedTeam,
    string? EscalationReason) : DomainEvent;
