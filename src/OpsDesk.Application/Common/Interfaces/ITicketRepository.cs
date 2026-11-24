using OpsDesk.Domain.Aggregates;

namespace OpsDesk.Application.Common.Interfaces;

public interface ITicketRepository
{
    Task<ServiceTicket?> GetByIdAsync(string id, CancellationToken ct = default);
    Task AddAsync(ServiceTicket ticket, CancellationToken ct = default);
    Task UpdateAsync(ServiceTicket ticket, CancellationToken ct = default);
}
