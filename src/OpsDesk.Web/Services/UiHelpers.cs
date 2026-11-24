using MudBlazor;
using OpsDesk.Application.Dtos;

namespace OpsDesk.Web.Services;

public static class UiHelpers
{
    public static Color PriorityColor(string? priority) => priority?.ToLowerInvariant() switch
    {
        "urgent" => Color.Error,
        "high" => Color.Warning,
        _ => Color.Info
    };

    public static Color StatusColor(string? status) => status?.ToLowerInvariant() switch
    {
        "open" => Color.Info,
        "returnedtorequester" => Color.Warning,
        "inprogress" => Color.Secondary,
        "pendingapproval" => Color.Tertiary,
        "resolved" => Color.Success,
        _ => Color.Default
    };

    public static string StatusLabel(string? status) => status?.ToLowerInvariant() switch
    {
        "returnedtorequester" => "Returned to Requester",
        "inprogress" => "In Progress",
        "pendingapproval" => "Pending Approval",
        _ => status ?? "Unknown"
    };

    public static (string Label, Color Color) SlaHealth(TicketSummaryDto ticket)
    {
        if (string.Equals(ticket.Status, "Resolved", StringComparison.OrdinalIgnoreCase))
            return ("Met", Color.Success);
        if (ticket.IsOverdue)
            return ("Breached", Color.Error);
        if (ticket.IsAtRisk)
            return ("At Risk", Color.Warning);
        return ("On Track", Color.Success);
    }

    public static string FormatTimeRemaining(DateTime dueDate, string status)
    {
        if (string.Equals(status, "Resolved", StringComparison.OrdinalIgnoreCase))
            return "Completed";

        var remaining = dueDate - DateTime.UtcNow;
        if (remaining.TotalMinutes < 0)
            return $"Overdue by {Math.Abs((int)remaining.TotalHours)}h";
        if (remaining.TotalHours < 24)
            return $"{Math.Max(1, (int)remaining.TotalHours)}h {remaining.Minutes}m left";
        return $"{(int)remaining.TotalDays}d {remaining.Hours}h left";
    }
}
