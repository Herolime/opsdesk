namespace OpsDesk.Application.Entities;

public sealed class Notification
{
    public long Id { get; set; }
    public string TicketId { get; set; } = default!;
    public string RecipientEmail { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string Category { get; set; } = "General";
    public bool IsRead { get; set; }
    public DateTime CreatedDate { get; set; }
}
