namespace OpsDesk.Domain.Entities;

public sealed class TicketNote
{
    public long Id { get; private set; }
    public string Author { get; private set; } = default!;
    public string NoteText { get; private set; } = default!;
    public DateTime CreatedDate { get; private set; }
    public bool IsNew { get; private set; } = true;

    public TicketNote(string author, string noteText)
    {
        Author = author;
        NoteText = noteText;
        CreatedDate = DateTime.UtcNow;
    }

    public TicketNote(long id, string author, string noteText, DateTime createdDate)
    {
        Id = id;
        Author = author;
        NoteText = noteText;
        CreatedDate = createdDate;
        IsNew = false;
    }

    private TicketNote() { }

    public void MarkPersisted(long id)
    {
        Id = id;
        IsNew = false;
    }
}
