using Microsoft.AspNetCore.Mvc;
using OpsDesk.Application.Common.Interfaces;
using OpsDesk.Application.Entities;

namespace OpsDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationRepository _notifications;

    public NotificationsController(INotificationRepository notifications) => _notifications = notifications;

    [HttpGet]
    public async Task<ActionResult<List<Notification>>> GetRecent([FromQuery] string? recipientEmail = null, [FromQuery] int limit = 30) =>
        Ok(await _notifications.GetRecentAsync(recipientEmail, limit));

    [HttpPost("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id)
    {
        await _notifications.MarkAsReadAsync(id);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead([FromQuery] string? recipientEmail = null)
    {
        await _notifications.MarkAllAsReadAsync(recipientEmail);
        return NoContent();
    }
}
