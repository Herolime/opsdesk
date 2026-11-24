namespace OpsDesk.Domain.ValueObjects;

public sealed record TicketId
{
    private const string Prefix = "OPS";

    public string Value { get; }

    private TicketId(string value) => Value = value;

    public static TicketId Create(int sequenceNumber) =>
        new($"{Prefix}-{sequenceNumber:D4}");

    public static TicketId Create(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("TicketId cannot be empty.", nameof(value))
            : new TicketId(value);

    public override string ToString() => Value;
}
