using MediatR;
using OpsDesk.Domain.Enums;

namespace OpsDesk.Application.Tickets.Commands;

public sealed record AssignSpecialistCommand(string TicketId, string SpecialistName) : IRequest;

public sealed record UpdateTicketStatusCommand(string TicketId, TicketStatus NewStatus) : IRequest;

public sealed record ChangePriorityCommand(string TicketId, Priority NewPriority) : IRequest;

public sealed record AddTicketMessageCommand(string TicketId, string SenderName, string SenderRole, string MessageText) : IRequest;

public sealed record AddTicketNoteCommand(string TicketId, string Author, string NoteText) : IRequest;

public sealed record SaveTechnicalReviewCommand : IRequest
{
    public string TicketId { get; init; } = default!;
    public string? ArchitectureReference { get; init; }
    public string? TechnicalAnalysis { get; init; }
    public string? OperationalRisks { get; init; }
    public string? ResolutionSummary { get; init; }
    public string? ReviewedBy { get; init; }
}

public sealed record EscalateTicketCommand(string TicketId, string EscalatedTeam, string? EscalationReason) : IRequest;
