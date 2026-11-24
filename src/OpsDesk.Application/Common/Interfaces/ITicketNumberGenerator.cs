namespace OpsDesk.Application.Common.Interfaces;

public interface ITicketNumberGenerator
{
    Task<string> NextAsync(CancellationToken ct = default);
}
