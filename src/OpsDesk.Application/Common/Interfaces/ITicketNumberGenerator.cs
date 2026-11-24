using OpsDesk.Domain.ValueObjects;

namespace OpsDesk.Application.Common.Interfaces;

public interface ITicketNumberGenerator
{
    Task<TicketId> NextAsync(CancellationToken ct = default);
}
