using System.Text;
using Dapper;
using OpsDesk.Application.Common.Interfaces;
using OpsDesk.Application.Dtos;
using OpsDesk.Common.Helpers;
using OpsDesk.Infrastructure.Persistence.Db;

namespace OpsDesk.Infrastructure.Persistence.Repositories;

public sealed class TicketQueryRepository : ITicketQueryRepository
{
    private readonly IDbConnectionFactory _factory;

    public TicketQueryRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<PagedResponse<TicketSummaryDto>> GetTicketsAsync(
        string? search = null,
        string? status = null,
        string? serviceStream = null,
        string? priority = null,
        string? requesterEmail = null,
        string? assignedSpecialist = null,
        int page = 1,
        int pageSize = 25)
    {
        using var connection = _factory.CreateConnection();
        var where = new StringBuilder("WHERE 1=1");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Append(" AND (Id LIKE @Search OR Title LIKE @Search OR RequesterName LIKE @Search OR Category LIKE @Search)");
            parameters.Add("Search", $"%{search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            where.Append(" AND Status = @Status");
            parameters.Add("Status", status);
        }
        if (!string.IsNullOrWhiteSpace(serviceStream))
        {
            where.Append(" AND ServiceStream = @ServiceStream");
            parameters.Add("ServiceStream", serviceStream);
        }
        if (!string.IsNullOrWhiteSpace(priority))
        {
            where.Append(" AND Priority = @Priority");
            parameters.Add("Priority", priority);
        }
        if (!string.IsNullOrWhiteSpace(requesterEmail))
        {
            where.Append(" AND LOWER(RequesterEmail) = LOWER(@RequesterEmail)");
            parameters.Add("RequesterEmail", requesterEmail);
        }
        if (!string.IsNullOrWhiteSpace(assignedSpecialist))
        {
            where.Append(" AND AssignedSpecialist = @AssignedSpecialist");
            parameters.Add("AssignedSpecialist", assignedSpecialist);
        }

        var total = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Tickets {where}", parameters);

        parameters.Add("Limit", pageSize);
        parameters.Add("Offset", (Math.Max(1, page) - 1) * pageSize);

        var items = (await connection.QueryAsync<TicketSummaryDto>($"""
            SELECT Id, CreatedDate, Priority, RequesterName, RequesterEmail, Department,
                   ServiceStream, Environment, Category, Title, Status, AssignedSpecialist,
                   ReturnReason, ReturnedBy, ReturnedAt, TatDueDate, CompletedDate, ClosedBy
            FROM Tickets
            {where}
            ORDER BY CreatedDate DESC
            LIMIT @Limit OFFSET @Offset
            """, parameters)).ToList();

        return new PagedResponse<TicketSummaryDto>
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<TicketDetailDto?> GetTicketByIdAsync(string id)
    {
        using var connection = _factory.CreateConnection();
        var detail = await connection.QuerySingleOrDefaultAsync<TicketDetailDto>("""
            SELECT Id, CreatedDate, Priority, RequesterName, RequesterEmail, Department,
                   ServiceStream, Environment, Category, Title, Description, TechnicalContext,
                   Status, AssignedSpecialist, ReturnReason, ReturnedBy, ReturnedAt,
                   TatDueDate, CompletedDate, ClosedBy
            FROM Tickets WHERE Id = @Id
            """, new { Id = id });

        if (detail is null) return null;

        detail.Review = await connection.QuerySingleOrDefaultAsync<TechnicalReviewDto>("""
            SELECT ArchitectureReference, TechnicalAnalysis, OperationalRisks, ResolutionSummary, ReviewedBy, UpdatedDate
            FROM TechnicalReviews WHERE TicketId = @Id
            """, new { Id = id });

        detail.Escalation = await connection.QuerySingleOrDefaultAsync<EscalationDto>("""
            SELECT EscalatedTeam, EscalationReason, EscalatedDate
            FROM Escalations WHERE TicketId = @Id ORDER BY Id DESC LIMIT 1
            """, new { Id = id });

        detail.Messages = (await connection.QueryAsync<TicketMessageDto>("""
            SELECT Id, SenderName, SenderRole, MessageText, CreatedDate
            FROM TicketMessages WHERE TicketId = @Id ORDER BY Id ASC
            """, new { Id = id })).ToList();

        detail.Notes = (await connection.QueryAsync<TicketNoteDto>("""
            SELECT Id, Author, NoteText, CreatedDate
            FROM TicketNotes WHERE TicketId = @Id ORDER BY Id ASC
            """, new { Id = id })).ToList();

        return detail;
    }

    public async Task<List<TriageTicketDto>> GetPendingTriageAsync()
    {
        using var connection = _factory.CreateConnection();
        var rows = await connection.QueryAsync<TriageTicketDto>("""
            SELECT t.Id, t.CreatedDate, t.Priority, t.RequesterName, t.Department,
                   t.ServiceStream, t.Environment, t.Category, t.Title, t.Description,
                   m.PrimarySpecialist AS SuggestedSpecialist
            FROM Tickets t
            LEFT JOIN StreamSpecialistMappings m
              ON m.CategoryName = t.Category AND m.ServiceStream = t.ServiceStream
            WHERE t.Status = 'Open' AND (t.AssignedSpecialist IS NULL OR t.AssignedSpecialist = '')
            ORDER BY CASE t.Priority WHEN 'Urgent' THEN 1 WHEN 'High' THEN 2 ELSE 3 END, t.CreatedDate ASC
            """);
        return rows.ToList();
    }

    public async Task<string?> SuggestSpecialistAsync(string category, string serviceStream)
    {
        using var connection = _factory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<string?>(
            "SELECT PrimarySpecialist FROM StreamSpecialistMappings WHERE CategoryName = @Category AND ServiceStream = @Stream LIMIT 1",
            new { Category = category, Stream = serviceStream });
    }

    public async Task<List<StreamSpecialistMappingDto>> GetStreamMappingsAsync()
    {
        using var connection = _factory.CreateConnection();
        var rows = await connection.QueryAsync<StreamSpecialistMappingDto>(
            "SELECT CategoryName, ServiceStream, PrimarySpecialist, BackupSpecialist FROM StreamSpecialistMappings ORDER BY ServiceStream, CategoryName");
        return rows.ToList();
    }

    public async Task<ReportSummaryDto> GetReportSummaryAsync()
    {
        using var connection = _factory.CreateConnection();
        var tickets = (await connection.QueryAsync<TicketSummaryDto>("""
            SELECT Id, CreatedDate, Priority, RequesterName, RequesterEmail, Department,
                   ServiceStream, Environment, Category, Title, Status, AssignedSpecialist,
                   ReturnReason, ReturnedBy, ReturnedAt, TatDueDate, CompletedDate, ClosedBy
            FROM Tickets
            """)).ToList();

        var resolved = tickets.Where(t => string.Equals(t.Status, "Resolved", StringComparison.OrdinalIgnoreCase)).ToList();
        var onTimeResolved = resolved.Count(t => t.CompletedDate is not null && t.CompletedDate.Value <= t.TatDueDate);
        var onTimeRate = resolved.Count == 0 ? 100.0 : Math.Round(100.0 * onTimeResolved / resolved.Count, 1);

        return new ReportSummaryDto
        {
            TotalTickets = tickets.Count,
            OpenTickets = tickets.Count(t => t.Status == "Open"),
            ReturnedTickets = tickets.Count(t => t.Status == "ReturnedToRequester"),
            InProgressTickets = tickets.Count(t => t.Status == "InProgress"),
            PendingApprovalTickets = tickets.Count(t => t.Status == "PendingApproval"),
            ResolvedTickets = resolved.Count,
            AtRiskTickets = tickets.Count(t => t.IsOverdue || t.IsAtRisk),
            OnTimeRate = onTimeRate,
            ByCategory = tickets.GroupBy(t => t.Category)
                .Select(g => new ReportRowDto { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count).ToList(),
            ByStatus = tickets.GroupBy(t => t.Status)
                .Select(g => new ReportRowDto { Label = g.Key, Count = g.Count() })
                .ToList(),
            ByServiceStream = tickets.GroupBy(t => t.ServiceStream)
                .Select(g => new ReportRowDto { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count).ToList(),
            BySpecialist = tickets
                .Where(t => !string.IsNullOrWhiteSpace(t.AssignedSpecialist))
                .GroupBy(t => t.AssignedSpecialist!)
                .Select(g =>
                {
                    var specResolved = g.Where(x => x.Status == "Resolved").ToList();
                    var specOnTime = specResolved.Count(x => x.CompletedDate is not null && x.CompletedDate.Value <= x.TatDueDate);
                    return new SpecialistThroughputDto
                    {
                        SpecialistName = g.Key,
                        AssignedCount = g.Count(x => x.Status is "InProgress" or "PendingApproval"),
                        PendingApprovalCount = g.Count(x => x.Status == "PendingApproval"),
                        ResolvedCount = specResolved.Count,
                        OnTimeRate = specResolved.Count == 0 ? 100.0 : Math.Round(100.0 * specOnTime / specResolved.Count, 1)
                    };
                })
                .OrderByDescending(x => x.AssignedCount)
                .ToList()
        };
    }
}
