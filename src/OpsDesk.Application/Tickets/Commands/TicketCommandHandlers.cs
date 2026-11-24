using MediatR;
using OpsDesk.Application.Common.Interfaces;

namespace OpsDesk.Application.Tickets.Commands;

public sealed class AssignSpecialistCommandHandler : IRequestHandler<AssignSpecialistCommand>
{
    private readonly ITicketRepository _repository;

    public AssignSpecialistCommandHandler(ITicketRepository repository) => _repository = repository;

    public async Task Handle(AssignSpecialistCommand request, CancellationToken ct)
    {
        var aggregate = await _repository.GetByIdAsync(request.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        aggregate.AssignSpecialist(request.SpecialistName);
        await _repository.UpdateAsync(aggregate, ct);
    }
}

public sealed class UpdateTicketStatusCommandHandler : IRequestHandler<UpdateTicketStatusCommand>
{
    private readonly ITicketRepository _repository;

    public UpdateTicketStatusCommandHandler(ITicketRepository repository) => _repository = repository;

    public async Task Handle(UpdateTicketStatusCommand request, CancellationToken ct)
    {
        var aggregate = await _repository.GetByIdAsync(request.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        aggregate.UpdateStatus(request.NewStatus);
        await _repository.UpdateAsync(aggregate, ct);
    }
}

public sealed class ChangePriorityCommandHandler : IRequestHandler<ChangePriorityCommand>
{
    private readonly ITicketRepository _repository;

    public ChangePriorityCommandHandler(ITicketRepository repository) => _repository = repository;

    public async Task Handle(ChangePriorityCommand request, CancellationToken ct)
    {
        var aggregate = await _repository.GetByIdAsync(request.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        aggregate.ChangePriority(request.NewPriority);
        await _repository.UpdateAsync(aggregate, ct);
    }
}

public sealed class AddTicketMessageCommandHandler : IRequestHandler<AddTicketMessageCommand>
{
    private readonly ITicketRepository _repository;

    public AddTicketMessageCommandHandler(ITicketRepository repository) => _repository = repository;

    public async Task Handle(AddTicketMessageCommand request, CancellationToken ct)
    {
        var aggregate = await _repository.GetByIdAsync(request.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        aggregate.AddMessage(request.SenderName, request.SenderRole, request.MessageText);
        await _repository.UpdateAsync(aggregate, ct);
    }
}

public sealed class AddTicketNoteCommandHandler : IRequestHandler<AddTicketNoteCommand>
{
    private readonly ITicketRepository _repository;

    public AddTicketNoteCommandHandler(ITicketRepository repository) => _repository = repository;

    public async Task Handle(AddTicketNoteCommand request, CancellationToken ct)
    {
        var aggregate = await _repository.GetByIdAsync(request.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        aggregate.AddInternalNote(request.Author, request.NoteText, isOperationsStaff: true);
        await _repository.UpdateAsync(aggregate, ct);
    }
}

public sealed class SaveTechnicalReviewCommandHandler : IRequestHandler<SaveTechnicalReviewCommand>
{
    private readonly ITicketRepository _repository;

    public SaveTechnicalReviewCommandHandler(ITicketRepository repository) => _repository = repository;

    public async Task Handle(SaveTechnicalReviewCommand request, CancellationToken ct)
    {
        var aggregate = await _repository.GetByIdAsync(request.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        aggregate.SetTechnicalReview(
            request.ArchitectureReference,
            request.TechnicalAnalysis,
            request.OperationalRisks,
            request.ResolutionSummary,
            request.ReviewedBy);

        await _repository.UpdateAsync(aggregate, ct);
    }
}

public sealed class EscalateTicketCommandHandler : IRequestHandler<EscalateTicketCommand>
{
    private readonly ITicketRepository _repository;

    public EscalateTicketCommandHandler(ITicketRepository repository) => _repository = repository;

    public async Task Handle(EscalateTicketCommand request, CancellationToken ct)
    {
        var aggregate = await _repository.GetByIdAsync(request.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        aggregate.EscalateToTeam(request.EscalatedTeam, request.EscalationReason);
        await _repository.UpdateAsync(aggregate, ct);
    }
}
