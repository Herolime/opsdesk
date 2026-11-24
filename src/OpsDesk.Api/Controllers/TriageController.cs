using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpsDesk.Application.Dtos;
using OpsDesk.Application.Tickets.Queries;

namespace OpsDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TriageController : ControllerBase
{
    private readonly IMediator _mediator;

    public TriageController(IMediator mediator) => _mediator = mediator;

    [HttpGet("pending")]
    public async Task<ActionResult<List<TriageTicketDto>>> GetPending() =>
        Ok(await _mediator.Send(new GetPendingTriageQuery()));

    [HttpGet("specialist-suggestions")]
    public async Task<ActionResult<string?>> SuggestSpecialist([FromQuery] string category, [FromQuery] string serviceStream)
    {
        var suggestion = await _mediator.Send(new SuggestSpecialistQuery(category, serviceStream));
        return Ok(suggestion);
    }

    [HttpGet("mappings")]
    public async Task<ActionResult<List<StreamSpecialistMappingDto>>> GetMappings() =>
        Ok(await _mediator.Send(new GetStreamMappingsQuery()));
}
