using OpsDesk.Application.Common.Interfaces;
using OpsDesk.Application.Enums;

namespace OpsDesk.Infrastructure.Services;

public sealed class BusinessDaySlaCalculator : IBusinessDaySlaCalculator
{
    public (int TargetTatDays, DateTime TatDueDate) Calculate(Priority priority, DateTime? from = null)
    {
        var start = from ?? DateTime.UtcNow;
        var days = priority switch
        {
            Priority.Urgent => 1,
            Priority.High => 3,
            Priority.Standard => 7,
            _ => 7
        };

        return (days, AddBusinessDays(start, days));
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
