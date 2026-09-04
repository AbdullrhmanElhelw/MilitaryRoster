using MilitaryRoster.Models.Entities;
using MilitaryRoster.Models.Enums;

namespace MilitaryRoster.Services;

public record StatusEvaluationResult(
    SoldierDutyStatus Status,
    string StatusDisplayName,
    string StatusDescription,
    int? CurrentDutyDayNumber,
    int RemainingDaysToLeave
);

public interface IRosterCalculationService
{
    DateOnly CalculateLeaveStartDate(DateOnly lastReturnDate, int dutyDays, int deductionDays);
    DateOnly CalculateExpectedReturnDate(DateOnly leaveStartDate, int leaveDays, int bonusDays = 0);
    StatusEvaluationResult EvaluateStatus(LeaveCycle currentCycle, SoldierType soldierType, DateOnly today);
}
