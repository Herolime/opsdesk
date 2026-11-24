using MediatR;
using OpsDesk.Application.Common.Interfaces;
using OpsDesk.Application.Entities;

namespace OpsDesk.Application.Tickets.Commands;

public sealed class AssignSpecialistCommandHandler : IRequestHandler<AssignSpecialistCommand>
{
    private readonly ITicketRepository _repository;
    private readonly INotificationRepository _notifications;

    public AssignSpecialistCommandHandler(ITicketRepository repository, INotificationRepository notifications)
    {
        _repository = repository;
        _notifications = notifications;
    }

    public async Task Handle(AssignSpecialistCommand request, CancellationToken ct)
    {
        var ticket = await _repository.GetByIdAsync(request.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        ticket.AssignSpecialist(request.SpecialistName);
        await _repository.UpdateAsync(ticket, ct);

        await _notifications.AddAsync(new Notification
        {
            TicketId = ticket.Id,
            RecipientEmail = ticket.RequesterEmail,
            Title = "Specialist Assigned",
            Message = $"{ticket.Id} was assigned to {request.SpecialistName}.",
            Category = "Assignment",
            CreatedDate = DateTime.UtcNow
        }, ct);
    }
}

public sealed class ReturnTicketToRequesterCommandHandler : IRequestHandler<ReturnTicketToRequesterCommand>
{
    private readonly ITicketRepository _repository;
    private readonly INotificationRepository _notifications;
    private readonly IEmailService _email;

    public ReturnTicketToRequesterCommandHandler(
        ITicketRepository repository,
        INotificationRepository notifications,
        IEmailService email)
    {
        _repository = repository;
        _notifications = notifications;
        _email = email;
    }

    public async Task Handle(ReturnTicketToRequesterCommand request, CancellationToken ct)
    {
        var ticket = await _repository.GetByIdAsync(request.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        ticket.ReturnToRequester(request.ReturnedBy, request.Reason);
        await _repository.UpdateAsync(ticket, ct);

        await _notifications.AddAsync(new Notification
        {
            TicketId = ticket.Id,
            RecipientEmail = ticket.RequesterEmail,
            Title = "Action Required: Ticket Returned",
            Message = $"{ticket.Id} was returned by {request.ReturnedBy}: {request.Reason}",
            Category = "Returned",
            CreatedDate = DateTime.UtcNow
        }, ct);

        await _email.SendEmailAsync(
            ticket.RequesterEmail,
            $"[OpsDesk] Action Required on {ticket.Id}",
            $"<p><strong>{request.ReturnedBy}</strong> returned <strong>{ticket.Id}</strong> for clarification:</p><blockquote>{request.Reason}</blockquote>",
            ticket.RequesterName,
            ct);
    }
}

public sealed class ResubmitTicketCommandHandler : IRequestHandler<ResubmitTicketCommand>
{
    private readonly ITicketRepository _repository;
    private readonly INotificationRepository _notifications;

    public ResubmitTicketCommandHandler(ITicketRepository repository, INotificationRepository notifications)
    {
        _repository = repository;
        _notifications = notifications;
    }

    public async Task Handle(ResubmitTicketCommand request, CancellationToken ct)
    {
        var ticket = await _repository.GetByIdAsync(request.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        ticket.Resubmit(request.Title, request.Description, request.TechnicalContext);
        await _repository.UpdateAsync(ticket, ct);

        await _notifications.AddAsync(new Notification
        {
            TicketId = ticket.Id,
            RecipientEmail = ticket.RequesterEmail,
            Title = "Ticket Resubmitted",
            Message = $"{ticket.Id} was updated and resubmitted by {ticket.RequesterName}.",
            Category = "Resubmitted",
            CreatedDate = DateTime.UtcNow
        }, ct);
    }
}

public sealed class SubmitForApprovalCommandHandler : IRequestHandler<SubmitForApprovalCommand>
{
    private readonly ITicketRepository _repository;
    private readonly INotificationRepository _notifications;

    public SubmitForApprovalCommandHandler(ITicketRepository repository, INotificationRepository notifications)
    {
        _repository = repository;
        _notifications = notifications;
    }

    public async Task Handle(SubmitForApprovalCommand request, CancellationToken ct)
    {
        var ticket = await _repository.GetByIdAsync(request.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        ticket.SubmitForApproval(request.SubmittedBy);
        await _repository.UpdateAsync(ticket, ct);

        await _notifications.AddAsync(new Notification
        {
            TicketId = ticket.Id,
            RecipientEmail = "alex.rivera@opsdesk.io",
            Title = "Pending Leader Approval",
            Message = $"{ticket.Id} technical analysis was submitted by {request.SubmittedBy} for approval.",
            Category = "Approval",
            CreatedDate = DateTime.UtcNow
        }, ct);
    }
}

public sealed class ReturnTicketToProgressCommandHandler : IRequestHandler<ReturnTicketToProgressCommand>
{
    private readonly ITicketRepository _repository;
    private readonly INotificationRepository _notifications;

    public ReturnTicketToProgressCommandHandler(ITicketRepository repository, INotificationRepository notifications)
    {
        _repository = repository;
        _notifications = notifications;
    }

    public async Task Handle(ReturnTicketToProgressCommand request, CancellationToken ct)
    {
        var ticket = await _repository.GetByIdAsync(request.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        ticket.ReturnToProgress(request.ReturnedBy, request.Feedback);
        await _repository.UpdateAsync(ticket, ct);

        await _notifications.AddAsync(new Notification
        {
            TicketId = ticket.Id,
            RecipientEmail = ticket.RequesterEmail,
            Title = "Analysis Returned to Specialist",
            Message = $"{ticket.Id} was returned to InProgress by {request.ReturnedBy}: {request.Feedback}",
            Category = "Returned",
            CreatedDate = DateTime.UtcNow
        }, ct);
    }
}

public sealed class ApproveAndCloseTicketCommandHandler : IRequestHandler<ApproveAndCloseTicketCommand>
{
    private readonly ITicketRepository _repository;
    private readonly INotificationRepository _notifications;
    private readonly IEmailService _email;

    public ApproveAndCloseTicketCommandHandler(
        ITicketRepository repository,
        INotificationRepository notifications,
        IEmailService email)
    {
        _repository = repository;
        _notifications = notifications;
        _email = email;
    }

    public async Task Handle(ApproveAndCloseTicketCommand request, CancellationToken ct)
    {
        var ticket = await _repository.GetByIdAsync(request.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        ticket.ApproveAndClose(request.ClosedBy, request.ResolutionSummary);
        await _repository.UpdateAsync(ticket, ct);

        await _notifications.AddAsync(new Notification
        {
            TicketId = ticket.Id,
            RecipientEmail = ticket.RequesterEmail,
            Title = "Ticket Resolved",
            Message = $"{ticket.Id} was approved and resolved by {request.ClosedBy}.",
            Category = "Resolved",
            CreatedDate = DateTime.UtcNow
        }, ct);

        await _email.SendEmailAsync(
            ticket.RequesterEmail,
            $"[OpsDesk] {ticket.Id} Resolved",
            $"<p>Your service request <strong>{ticket.Id}</strong> has been approved and closed by {request.ClosedBy}.</p>",
            ticket.RequesterName,
            ct);
    }
}

public sealed class UpdateTicketStatusCommandHandler : IRequestHandler<UpdateTicketStatusCommand>
{
    private readonly ITicketRepository _repository;

    public UpdateTicketStatusCommandHandler(ITicketRepository repository) => _repository = repository;

    public async Task Handle(UpdateTicketStatusCommand request, CancellationToken ct)
    {
        var ticket = await _repository.GetByIdAsync(request.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        ticket.UpdateStatus(request.NewStatus);
        await _repository.UpdateAsync(ticket, ct);
    }
}

public sealed class ChangePriorityCommandHandler : IRequestHandler<ChangePriorityCommand>
{
    private readonly ITicketRepository _repository;
    private readonly IBusinessDaySlaCalculator _sla;

    public ChangePriorityCommandHandler(ITicketRepository repository, IBusinessDaySlaCalculator sla)
    {
        _repository = repository;
        _sla = sla;
    }

    public async Task Handle(ChangePriorityCommand request, CancellationToken ct)
    {
        var ticket = await _repository.GetByIdAsync(request.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        var (tatDays, tatDueDate) = _sla.Calculate(request.NewPriority);
        ticket.ChangePriority(request.NewPriority, tatDays, tatDueDate);
        await _repository.UpdateAsync(ticket, ct);
    }
}

public sealed class AddTicketMessageCommandHandler : IRequestHandler<AddTicketMessageCommand>
{
    private readonly ITicketRepository _repository;

    public AddTicketMessageCommandHandler(ITicketRepository repository) => _repository = repository;

    public async Task Handle(AddTicketMessageCommand request, CancellationToken ct)
    {
        var ticket = await _repository.GetByIdAsync(request.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        ticket.AddMessage(request.SenderName, request.SenderRole, request.MessageText);
        await _repository.UpdateAsync(ticket, ct);
    }
}

public sealed class AddTicketNoteCommandHandler : IRequestHandler<AddTicketNoteCommand>
{
    private readonly ITicketRepository _repository;

    public AddTicketNoteCommandHandler(ITicketRepository repository) => _repository = repository;

    public async Task Handle(AddTicketNoteCommand request, CancellationToken ct)
    {
        var ticket = await _repository.GetByIdAsync(request.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        ticket.AddInternalNote(request.Author, request.NoteText);
        await _repository.UpdateAsync(ticket, ct);
    }
}

public sealed class SaveTechnicalReviewCommandHandler : IRequestHandler<SaveTechnicalReviewCommand>
{
    private readonly ITicketRepository _repository;

    public SaveTechnicalReviewCommandHandler(ITicketRepository repository) => _repository = repository;

    public async Task Handle(SaveTechnicalReviewCommand request, CancellationToken ct)
    {
        var ticket = await _repository.GetByIdAsync(request.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        ticket.SetTechnicalReview(
            request.ArchitectureReference,
            request.TechnicalAnalysis,
            request.OperationalRisks,
            request.ResolutionSummary,
            request.ReviewedBy);

        await _repository.UpdateAsync(ticket, ct);
    }
}

public sealed class EscalateTicketCommandHandler : IRequestHandler<EscalateTicketCommand>
{
    private readonly ITicketRepository _repository;

    public EscalateTicketCommandHandler(ITicketRepository repository) => _repository = repository;

    public async Task Handle(EscalateTicketCommand request, CancellationToken ct)
    {
        var ticket = await _repository.GetByIdAsync(request.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        ticket.EscalateToTeam(request.EscalatedTeam, request.EscalationReason);
        await _repository.UpdateAsync(ticket, ct);
    }
}
