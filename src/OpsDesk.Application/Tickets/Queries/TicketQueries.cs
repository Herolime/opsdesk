using MediatR;
using OpsDesk.Application.Common.Interfaces;
using OpsDesk.Application.Dtos;
using OpsDesk.Common.Helpers;

namespace OpsDesk.Application.Tickets.Queries;

public sealed record GetTicketsQuery(
    string? Search = null,
    string? Status = null,
    string? ServiceStream = null,
    string? Priority = null,
    string? RequesterEmail = null,
    string? AssignedSpecialist = null,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResponse<TicketSummaryDto>>;

public sealed record GetTicketByIdQuery(string TicketId) : IRequest<TicketDetailDto?>;

public sealed record GetPendingTriageQuery : IRequest<List<TriageTicketDto>>;

public sealed record SuggestSpecialistQuery(string Category, string ServiceStream) : IRequest<string?>;

public sealed record GetStreamMappingsQuery : IRequest<List<StreamSpecialistMappingDto>>;

public sealed record GetReportSummaryQuery : IRequest<ReportSummaryDto>;

public sealed class TicketQueryHandlers :
    IRequestHandler<GetTicketsQuery, PagedResponse<TicketSummaryDto>>,
    IRequestHandler<GetTicketByIdQuery, TicketDetailDto?>,
    IRequestHandler<GetPendingTriageQuery, List<TriageTicketDto>>,
    IRequestHandler<SuggestSpecialistQuery, string?>,
    IRequestHandler<GetStreamMappingsQuery, List<StreamSpecialistMappingDto>>,
    IRequestHandler<GetReportSummaryQuery, ReportSummaryDto>
{
    private readonly ITicketQueryRepository _queries;

    public TicketQueryHandlers(ITicketQueryRepository queries) => _queries = queries;

    public Task<PagedResponse<TicketSummaryDto>> Handle(GetTicketsQuery q, CancellationToken ct) =>
        _queries.GetTicketsAsync(q.Search, q.Status, q.ServiceStream, q.Priority, q.RequesterEmail, q.AssignedSpecialist, q.Page, q.PageSize);

    public Task<TicketDetailDto?> Handle(GetTicketByIdQuery q, CancellationToken ct) =>
        _queries.GetTicketByIdAsync(q.TicketId);

    public Task<List<TriageTicketDto>> Handle(GetPendingTriageQuery q, CancellationToken ct) =>
        _queries.GetPendingTriageAsync();

    public Task<string?> Handle(SuggestSpecialistQuery q, CancellationToken ct) =>
        _queries.SuggestSpecialistAsync(q.Category, q.ServiceStream);

    public Task<List<StreamSpecialistMappingDto>> Handle(GetStreamMappingsQuery q, CancellationToken ct) =>
        _queries.GetStreamMappingsAsync();

    public Task<ReportSummaryDto> Handle(GetReportSummaryQuery q, CancellationToken ct) =>
        _queries.GetReportSummaryAsync();
}
