using MediatR;
using MudBlazor;
using OpsDesk.Application.Tickets.Queries;

namespace OpsDesk.Web.Services;

public sealed class NotificationState
{
    private readonly List<NotificationItem> _alerts = new();
    private bool _loaded;

    public IReadOnlyList<NotificationItem> Alerts => _alerts;
    public int UnreadCount => _alerts.Count(a => !a.IsRead);

    public event Action? OnChange;

    public async Task EnsureLoadedAsync(IMediator mediator)
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        await RefreshAsync(mediator);
    }

    public async Task RefreshAsync(IMediator mediator)
    {
        var readIds = _alerts.Where(a => a.IsRead).Select(a => (a.TicketId, a.Title)).ToHashSet();
        _alerts.Clear();

        var tickets = (await mediator.Send(new GetTicketsQuery(PageSize: 10))).Items;
        foreach (var t in tickets)
        {
            if (t.IsOverdue || t.IsAtRisk)
            {
                var title = "SLA Threshold Alert";
                _alerts.Add(new NotificationItem(
                    t.Id,
                    title,
                    $"{t.Title} — {UiHelpers.FormatTimeRemaining(t.TatDueDate, t.Status)}.",
                    Icons.Material.Filled.WarningAmber,
                    Color.Warning)
                {
                    IsRead = readIds.Contains((t.Id, title))
                });
            }
            else if (t.Status == "Open" && string.IsNullOrWhiteSpace(t.AssignedSpecialist))
            {
                var title = "Pending Triage Routing";
                _alerts.Add(new NotificationItem(
                    t.Id,
                    title,
                    $"{t.Title} ({t.ServiceStream}) is awaiting specialist assignment.",
                    Icons.Material.Filled.Inbox,
                    Color.Info)
                {
                    IsRead = readIds.Contains((t.Id, title))
                });
            }
        }

        OnChange?.Invoke();
    }

    public void MarkAsRead(NotificationItem item)
    {
        if (!item.IsRead)
        {
            item.IsRead = true;
            OnChange?.Invoke();
        }
    }

    public void MarkAllRead()
    {
        var changed = false;
        foreach (var alert in _alerts)
        {
            if (!alert.IsRead)
            {
                alert.IsRead = true;
                changed = true;
            }
        }

        if (changed)
        {
            OnChange?.Invoke();
        }
    }
}

public sealed class NotificationItem(string ticketId, string title, string message, string icon, Color color)
{
    public string TicketId { get; } = ticketId;
    public string Title { get; } = title;
    public string Message { get; } = message;
    public string Icon { get; } = icon;
    public Color Color { get; } = color;
    public bool IsRead { get; set; }
}
