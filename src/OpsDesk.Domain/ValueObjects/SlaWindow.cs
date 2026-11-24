using OpsDesk.Domain.Enums;

namespace OpsDesk.Domain.ValueObjects;

public sealed record SlaWindow
{
    public int TargetTatDays { get; }
    public DateTime TatDueDate { get; }

    private SlaWindow(int targetTatDays, DateTime tatDueDate)
    {
        TargetTatDays = targetTatDays;
        TatDueDate = tatDueDate;
    }

    public static SlaWindow FromStored(int targetTatDays, DateTime tatDueDate) =>
        new(targetTatDays, tatDueDate);

    public static SlaWindow Create(Priority priority, DateTime? from = null)
    {
        var start = from ?? DateTime.UtcNow;
        var days = priority switch
        {
            Priority.Urgent => 1,
            Priority.High => 3,
            Priority.Standard => 7,
            _ => 7
        };

        return new SlaWindow(days, AddBusinessDays(start, days));
    }

    private static DateTime AddBusinessDays(DateTime start, int days)
    {
        var date = start;
        var added = 0;
        while (added < days)
        {
            date = date.AddDays(1);
            if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                added++;
        }
        return date;
    }
}
