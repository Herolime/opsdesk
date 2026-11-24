using OpsDesk.Application.Entities;

namespace OpsDesk.Application.Common.Interfaces;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(string id, CancellationToken ct = default);
    Task AddAsync(Ticket ticket, CancellationToken ct = default);
    Task UpdateAsync(Ticket ticket, CancellationToken ct = default);
}
