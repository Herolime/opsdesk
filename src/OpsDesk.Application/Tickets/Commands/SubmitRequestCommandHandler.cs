using MediatR;
using OpsDesk.Application.Common.Interfaces;
using OpsDesk.Application.Entities;

namespace OpsDesk.Application.Tickets.Commands;

public sealed class SubmitRequestCommandHandler : IRequestHandler<SubmitRequestCommand, string>
{
    private readonly ITicketNumberGenerator _idGenerator;
    private readonly ITicketRepository _repository;
    private readonly IBusinessDaySlaCalculator _slaCalculator;
    private readonly INotificationRepository _notifications;
    private readonly IEmailService _emailService;

    public SubmitRequestCommandHandler(
        ITicketNumberGenerator idGenerator,
        ITicketRepository repository,
        IBusinessDaySlaCalculator slaCalculator,
        INotificationRepository notifications,
        IEmailService emailService)
    {
        _idGenerator = idGenerator;
        _repository = repository;
        _slaCalculator = slaCalculator;
        _notifications = notifications;
        _emailService = emailService;
    }

    public async Task<string> Handle(SubmitRequestCommand request, CancellationToken ct)
    {
        var ticketId = await _idGenerator.NextAsync(ct);
        var (tatDays, tatDueDate) = _slaCalculator.Calculate(request.Priority);

        var ticket = Ticket.Create(
            ticketId,
            request.RequesterName,
            request.RequesterEmail,
            request.Department,
            request.Priority,
            request.ServiceStream,
            request.Environment,
            request.Category,
            request.Title,
            request.Description,
            tatDays,
            tatDueDate,
            request.TechnicalContext);

        await _repository.AddAsync(ticket, ct);

        await _notifications.AddAsync(new Notification
        {
            TicketId = ticketId,
            RecipientEmail = request.RequesterEmail,
            Title = "Service Request Submitted",
            Message = $"{ticketId}: {request.Title} has been queued for {request.ServiceStream} triage.",
            Category = "Submitted",
            CreatedDate = DateTime.UtcNow
        }, ct);

        await _emailService.SendEmailAsync(
            request.RequesterEmail,
            $"[OpsDesk] {ticketId} Submitted: {request.Title}",
            $"<p>Your service request <strong>{ticketId}</strong> has been received with target SLA of {tatDays} business day(s).</p>",
            request.RequesterName,
            ct);

        return ticket.Id;
    }
}
