using MediatR;
using OpsDesk.Application.Common.Interfaces;
using OpsDesk.Domain.Aggregates;
using OpsDesk.Domain.ValueObjects;

namespace OpsDesk.Application.Tickets.Commands;

public sealed class SubmitRequestCommandHandler : IRequestHandler<SubmitRequestCommand, string>
{
    private readonly ITicketNumberGenerator _idGenerator;
    private readonly ITicketRepository _repository;

    public SubmitRequestCommandHandler(ITicketNumberGenerator idGenerator, ITicketRepository repository)
    {
        _idGenerator = idGenerator;
        _repository = repository;
    }

    public async Task<string> Handle(SubmitRequestCommand request, CancellationToken ct)
    {
        var ticketId = await _idGenerator.NextAsync(ct);
        var requester = new RequesterInfo(request.RequesterName, request.RequesterEmail, request.Department);

        var ticket = ServiceTicket.Create(
            ticketId,
            requester,
            request.Priority,
            request.ServiceStream,
            request.Environment,
            request.Category,
            request.Title,
            request.Description,
            request.TechnicalContext);

        await _repository.AddAsync(ticket, ct);
        return ticket.Id.Value;
    }
}
