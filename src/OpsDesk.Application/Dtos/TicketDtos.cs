namespace OpsDesk.Application.Dtos;

public class TicketSummaryDto
{
    public string Id { get; set; } = default!;
    public string CreatedDate { get; set; } = default!;
    public string Priority { get; set; } = default!;
    public string RequesterName { get; set; } = default!;
    public string RequesterEmail { get; set; } = default!;
    public string Department { get; set; } = default!;
    public string ServiceStream { get; set; } = default!;
    public string Environment { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? AssignedSpecialist { get; set; }
    public DateTime TatDueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public int Total { get; set; }
    public bool IsOverdue =>
        DateTime.UtcNow > TatDueDate &&
        !string.Equals(Status, "Resolved", StringComparison.OrdinalIgnoreCase);

    public bool IsAtRisk =>
        !IsOverdue &&
        !string.Equals(Status, "Resolved", StringComparison.OrdinalIgnoreCase) &&
        (TatDueDate - DateTime.UtcNow).TotalHours <= 24;
}

public sealed class TicketDetailDto : TicketSummaryDto
{
    public string Description { get; set; } = default!;
    public string? TechnicalContext { get; set; }
    public TechnicalReviewDto? Review { get; set; }
    public EscalationDto? Escalation { get; set; }
    public List<TicketMessageDto> Messages { get; set; } = new();
    public List<TicketNoteDto> Notes { get; set; } = new();
}

public sealed class TechnicalReviewDto
{
    public string? ArchitectureReference { get; set; }
    public string? TechnicalAnalysis { get; set; }
    public string? OperationalRisks { get; set; }
    public string? ResolutionSummary { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public sealed class TicketMessageDto
{
    public long Id { get; set; }
    public string SenderName { get; set; } = default!;
    public string SenderRole { get; set; } = default!;
    public string MessageText { get; set; } = default!;
    public DateTime CreatedDate { get; set; }
}

public sealed class TicketNoteDto
{
    public long Id { get; set; }
    public string Author { get; set; } = default!;
    public string NoteText { get; set; } = default!;
    public DateTime CreatedDate { get; set; }
}

public sealed class EscalationDto
{
    public string EscalatedTeam { get; set; } = default!;
    public string? EscalationReason { get; set; }
    public DateTime EscalatedDate { get; set; }
}

public sealed class TriageTicketDto
{
    public string Id { get; set; } = default!;
    public string CreatedDate { get; set; } = default!;
    public string Priority { get; set; } = default!;
    public string RequesterName { get; set; } = default!;
    public string Department { get; set; } = default!;
    public string ServiceStream { get; set; } = default!;
    public string Environment { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string? SuggestedSpecialist { get; set; }
}

public sealed class PagedResult<T>
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<T> Items { get; set; } = new();
}

public sealed class ReportSummaryDto
{
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int PendingReviewTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int AtRiskTickets { get; set; }
    public double OnTimeRate { get; set; }
    public List<ReportRowDto> ByCategory { get; set; } = new();
    public List<ReportRowDto> ByStatus { get; set; } = new();
    public List<ReportRowDto> ByServiceStream { get; set; } = new();
    public List<SpecialistThroughputDto> BySpecialist { get; set; } = new();
}

public sealed class ReportRowDto
{
    public string Label { get; set; } = default!;
    public int Count { get; set; }
}

public sealed class SpecialistThroughputDto
{
    public string SpecialistName { get; set; } = default!;
    public int AssignedCount { get; set; }
    public int ResolvedCount { get; set; }
    public double OnTimeRate { get; set; }
}

public sealed class StreamSpecialistMappingDto
{
    public string CategoryName { get; set; } = default!;
    public string ServiceStream { get; set; } = default!;
    public string PrimarySpecialist { get; set; } = default!;
    public string? BackupSpecialist { get; set; }
}
