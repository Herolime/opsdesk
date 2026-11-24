namespace OpsDesk.Domain.Entities;

public sealed class TicketMessage
{
    public long Id { get; private set; }
    public string SenderName { get; private set; } = default!;
    public string SenderRole { get; private set; } = default!;
    public string MessageText { get; private set; } = default!;
    public DateTime CreatedDate { get; private set; }
    public bool IsNew { get; private set; } = true;

    public TicketMessage(string senderName, string senderRole, string messageText)
    {
        SenderName = senderName;
        SenderRole = senderRole;
        MessageText = messageText;
        CreatedDate = DateTime.UtcNow;
    }

    public TicketMessage(long id, string senderName, string senderRole, string messageText, DateTime createdDate)
    {
        Id = id;
        SenderName = senderName;
        SenderRole = senderRole;
        MessageText = messageText;
        CreatedDate = createdDate;
        IsNew = false;
    }

    private TicketMessage() { }

    public void MarkPersisted(long id)
    {
        Id = id;
        IsNew = false;
    }
}
