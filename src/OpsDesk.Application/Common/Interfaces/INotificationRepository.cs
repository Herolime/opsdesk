using OpsDesk.Application.Entities;

namespace OpsDesk.Application.Common.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task<List<Notification>> GetRecentAsync(string? recipientEmail = null, int limit = 30, CancellationToken ct = default);
    Task MarkAsReadAsync(long id, CancellationToken ct = default);
    Task MarkAllAsReadAsync(string? recipientEmail = null, CancellationToken ct = default);
}
