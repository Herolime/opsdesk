using FluentValidation;
using OpsDesk.Application.Tickets.Commands;

namespace OpsDesk.Application.Validation;

public sealed class SubmitRequestCommandValidator : AbstractValidator<SubmitRequestCommand>
{
    public SubmitRequestCommandValidator()
    {
        RuleFor(x => x.RequesterName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.RequesterEmail).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Department).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Description).NotEmpty();
    }
}

public sealed class AssignSpecialistCommandValidator : AbstractValidator<AssignSpecialistCommand>
{
    public AssignSpecialistCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.SpecialistName).NotEmpty().MaximumLength(150);
    }
}

public sealed class AddTicketMessageCommandValidator : AbstractValidator<AddTicketMessageCommand>
{
    public AddTicketMessageCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.SenderName).NotEmpty();
        RuleFor(x => x.SenderRole).NotEmpty();
        RuleFor(x => x.MessageText).NotEmpty();
    }
}

public sealed class AddTicketNoteCommandValidator : AbstractValidator<AddTicketNoteCommand>
{
    public AddTicketNoteCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Author).NotEmpty();
        RuleFor(x => x.NoteText).NotEmpty();
    }
}

public sealed class EscalateTicketCommandValidator : AbstractValidator<EscalateTicketCommand>
{
    public EscalateTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.EscalatedTeam).NotEmpty().MaximumLength(150);
    }
}
