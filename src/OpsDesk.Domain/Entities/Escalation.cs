namespace OpsDesk.Domain.Entities;

public sealed class Escalation
{
    public long Id { get; private set; }
    public string EscalatedTeam { get; private set; } = default!;
    public string? EscalationReason { get; private set; }
    public DateTime EscalatedDate { get; private set; }

    public Escalation(string escalatedTeam, string? escalationReason)
    {
        EscalatedTeam = escalatedTeam;
        EscalationReason = escalationReason;
        EscalatedDate = DateTime.UtcNow;
    }

    private Escalation() { }
}
