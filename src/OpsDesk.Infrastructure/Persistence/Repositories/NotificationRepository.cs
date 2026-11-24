using Dapper;
using OpsDesk.Application.Common.Interfaces;
using OpsDesk.Application.Entities;
using OpsDesk.Infrastructure.Persistence.Db;

namespace OpsDesk.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly IDbConnectionFactory _factory;

    public NotificationRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task AddAsync(Notification notification, CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        await connection.ExecuteAsync("""
            INSERT INTO Notifications (TicketId, RecipientEmail, Title, Message, Category, IsRead, CreatedDate)
            VALUES (@TicketId, @RecipientEmail, @Title, @Message, @Category, @IsRead, @CreatedDate)
            """,
            new
            {
                notification.TicketId,
                notification.RecipientEmail,
                notification.Title,
                notification.Message,
                notification.Category,
                IsRead = notification.IsRead ? 1 : 0,
                CreatedDate = notification.CreatedDate.ToString("O")
            });
    }

    public async Task<List<Notification>> GetRecentAsync(string? recipientEmail = null, int limit = 30, CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        var rows = await connection.QueryAsync<Notification>("""
            SELECT Id, TicketId, RecipientEmail, Title, Message, Category, IsRead, CreatedDate
            FROM Notifications
            WHERE (@RecipientEmail IS NULL OR LOWER(RecipientEmail) = LOWER(@RecipientEmail))
            ORDER BY Id DESC
            LIMIT @Limit
            """, new { RecipientEmail = recipientEmail, Limit = limit });
        return rows.ToList();
    }

    public async Task MarkAsReadAsync(long id, CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        await connection.ExecuteAsync("UPDATE Notifications SET IsRead = 1 WHERE Id = @Id", new { Id = id });
    }

    public async Task MarkAllAsReadAsync(string? recipientEmail = null, CancellationToken ct = default)
    {
        using var connection = _factory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE Notifications SET IsRead = 1 WHERE (@RecipientEmail IS NULL OR LOWER(RecipientEmail) = LOWER(@RecipientEmail))",
            new { RecipientEmail = recipientEmail });
    }
}
