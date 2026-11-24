using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpsDesk.Application.Dtos;
using OpsDesk.Application.Tickets.Queries;

namespace OpsDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("summary")]
    public async Task<ActionResult<ReportSummaryDto>> GetSummary() =>
        Ok(await _mediator.Send(new GetReportSummaryQuery()));
}
