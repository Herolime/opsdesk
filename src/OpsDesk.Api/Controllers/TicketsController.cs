using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpsDesk.Application.Dtos;
using OpsDesk.Application.Tickets.Commands;
using OpsDesk.Application.Tickets.Queries;
using OpsDesk.Domain.Enums;

namespace OpsDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<PagedResult<TicketSummaryDto>>> GetTickets(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? serviceStream = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? requesterEmail = null,
        [FromQuery] string? assignedSpecialist = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await _mediator.Send(new GetTicketsQuery(
            search, status, serviceStream, priority, requesterEmail, assignedSpecialist, page, pageSize));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TicketDetailDto>> GetById(string id)
    {
        var detail = await _mediator.Send(new GetTicketByIdQuery(id));
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost]
    public async Task<ActionResult<string>> Submit([FromBody] SubmitRequestCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id}/assign")]
    public async Task<IActionResult> Assign(string id, [FromBody] AssignSpecialistRequest request)
    {
        await _mediator.Send(new AssignSpecialistCommand(id, request.SpecialistName));
        return NoContent();
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStatusRequest request)
    {
        await _mediator.Send(new UpdateTicketStatusCommand(id, request.NewStatus));
        return NoContent();
    }

    [HttpPut("{id}/priority")]
    public async Task<IActionResult> ChangePriority(string id, [FromBody] ChangePriorityRequest request)
    {
        await _mediator.Send(new ChangePriorityCommand(id, request.NewPriority));
        return NoContent();
    }

    [HttpPost("{id}/messages")]
    public async Task<IActionResult> AddMessage(string id, [FromBody] AddMessageRequest request)
    {
        await _mediator.Send(new AddTicketMessageCommand(id, request.SenderName, request.SenderRole, request.MessageText));
        return NoContent();
    }

    [HttpPost("{id}/notes")]
    public async Task<IActionResult> AddNote(string id, [FromBody] AddNoteRequest request)
    {
        await _mediator.Send(new AddTicketNoteCommand(id, request.Author, request.NoteText));
        return NoContent();
    }

    [HttpPut("{id}/review")]
    public async Task<IActionResult> SaveReview(string id, [FromBody] SaveTechnicalReviewCommand command)
    {
        await _mediator.Send(command with { TicketId = id });
        return NoContent();
    }

    [HttpPost("{id}/escalate")]
    public async Task<IActionResult> Escalate(string id, [FromBody] EscalateRequest request)
    {
        await _mediator.Send(new EscalateTicketCommand(id, request.EscalatedTeam, request.EscalationReason));
        return NoContent();
    }
}

public sealed record AssignSpecialistRequest(string SpecialistName);
public sealed record UpdateStatusRequest(TicketStatus NewStatus);
public sealed record ChangePriorityRequest(Priority NewPriority);
public sealed record AddMessageRequest(string SenderName, string SenderRole, string MessageText);
public sealed record AddNoteRequest(string Author, string NoteText);
public sealed record EscalateRequest(string EscalatedTeam, string? EscalationReason);
