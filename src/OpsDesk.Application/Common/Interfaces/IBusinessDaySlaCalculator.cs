using OpsDesk.Application.Enums;

namespace OpsDesk.Application.Common.Interfaces;

public interface IBusinessDaySlaCalculator
{
    (int TargetTatDays, DateTime TatDueDate) Calculate(Priority priority, DateTime? from = null);
}
