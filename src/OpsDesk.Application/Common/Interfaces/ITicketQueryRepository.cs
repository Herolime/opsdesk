using OpsDesk.Application.Dtos;

namespace OpsDesk.Application.Common.Interfaces;

public interface ITicketQueryRepository
{
    Task<PagedResult<TicketSummaryDto>> GetTicketsAsync(
        string? search = null,
        string? status = null,
        string? serviceStream = null,
        string? priority = null,
        string? requesterEmail = null,
        string? assignedSpecialist = null,
        int page = 1,
        int pageSize = 25);

    Task<TicketDetailDto?> GetTicketByIdAsync(string id);
    Task<List<TriageTicketDto>> GetPendingTriageAsync();
    Task<string?> SuggestSpecialistAsync(string category, string serviceStream);
    Task<List<StreamSpecialistMappingDto>> GetStreamMappingsAsync();
    Task<ReportSummaryDto> GetReportSummaryAsync();
}
