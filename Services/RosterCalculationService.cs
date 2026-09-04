using MilitaryRoster.Models.Entities;
using MilitaryRoster.Models.Enums;

namespace MilitaryRoster.Services;

public class RosterCalculationService : IRosterCalculationService
{
    /// <summary>
    /// حساب موعد النزول القادم بدقة:
    /// يوم العودة لا يُحسب كيوم تواجد؛ يبدأ العداد الفعلي من اليوم التالي.
    /// LeaveStartDate = LastReturnDate + 1 + DutyDays + DeductionDays
    /// الخصم يزيد أيام التواجد كعقوبة (أو يؤخر النزول).
    /// </summary>
    public DateOnly CalculateLeaveStartDate(DateOnly lastReturnDate, int dutyDays, int deductionDays)
    {
        int totalDaysToAdd = (dutyDays + 1) + deductionDays;
        return lastReturnDate.AddDays(totalDaysToAdd);
    }

    /// <summary>
    /// حساب موعد العودة القادم المخطط:
    /// ExpectedReturnDate = LeaveStartDate + LeaveDays + BonusDays
    /// المنحة تُضاف مباشرة على أيام الإجازة وتزيد من ميعاد الرجوع (الاستلام).
    /// </summary>
    public DateOnly CalculateExpectedReturnDate(DateOnly leaveStartDate, int leaveDays, int bonusDays = 0)
    {
        return leaveStartDate.AddDays(leaveDays + bonusDays);
    }

    /// <summary>
    /// تقييم الحالة اللحظية للموظف ديناميكياً مقارنة بتاريخ اليوم (Pure Function)
    /// لا يتم حفظ الحالة اليومية في قاعدة البيانات؛ الحساب فوري ومصدر الحقيقة هو التواريخ.
    /// </summary>
    public StatusEvaluationResult EvaluateStatus(LeaveCycle currentCycle, SoldierType soldierType, DateOnly today)
    {
        var leaveStartDate = currentCycle.LeaveStartDate;
        var returnDate = currentCycle.ActualReturnDate ?? currentCycle.ExpectedReturnDate;
        int remainingDaysToLeave = Math.Max(0, leaveStartDate.DayNumber - today.DayNumber);

        // 1. متأخر عن العودة (Overdue): تجاوز موعد العودة المخطط ولم تسجل العودة الفعلية
        if (!currentCycle.ActualReturnDate.HasValue && currentCycle.ActualLeaveDate.HasValue && today > currentCycle.ExpectedReturnDate)
        {
            int daysOverdue = today.DayNumber - currentCycle.ExpectedReturnDate.DayNumber;
            return new StatusEvaluationResult(
                SoldierDutyStatus.OverdueReturn,
                "متأخر عن العودة",
                $"متأخر عن العودة بمقدار {daysOverdue} يوم",
                null,
                0
            );
        }

        // 2. في إجازة حالياً (OnLeave): نزل فعلياً وتاريخ اليوم قبل تاريخ العودة
        if (currentCycle.ActualLeaveDate.HasValue && today >= currentCycle.ActualLeaveDate.Value && today < returnDate)
        {
            int leaveDayNum = (today.DayNumber - currentCycle.ActualLeaveDate.Value.DayNumber) + 1;
            return new StatusEvaluationResult(
                SoldierDutyStatus.OnLeave,
                "في إجازة",
                $"في إجازة (اليوم {leaveDayNum} من {soldierType.LeaveDays})",
                null,
                0
            );
        }

        // 3. موعد العودة اليوم (ReturnToday): اليوم هو موعد العودة/الاستلام إذا كان في إجازة ولم تسجل عودته بعد
        if (currentCycle.ActualLeaveDate.HasValue && !currentCycle.ActualReturnDate.HasValue && today == currentCycle.ExpectedReturnDate)
        {
            return new StatusEvaluationResult(
                SoldierDutyStatus.ReturnToday,
                "عودة اليوم",
                "عودة اليوم (استلام الخدمة - لا يُحسب تواجد)",
                null,
                remainingDaysToLeave
            );
        }

        // 4. موعد النزول اليوم (LeaveToday)
        if (today == leaveStartDate || (today > leaveStartDate && !currentCycle.ActualLeaveDate.HasValue))
        {
            return new StatusEvaluationResult(
                SoldierDutyStatus.LeaveToday,
                "نزول اليوم",
                "موعد النزول اليوم (جاهز للتحرك)",
                soldierType.DutyDays,
                0
            );
        }

        // 5. مستحق النزول غداً (EligibleForLeave - تجهيز وعمل البند قبلها بيوم)
        if (today == leaveStartDate.AddDays(-1))
        {
            return new StatusEvaluationResult(
                SoldierDutyStatus.EligibleForLeave,
                "نزول غداً (مستحق البند)",
                "غداً موعد النزول (مسموح بالنزول غداً - تجهيز البند)",
                soldierType.DutyDays - 1,
                1
            );
        }

        // 6. متواجد بالخدمة (OnDuty): احتساب رقم يوم التواجد الفعلي
        var firstDutyDay = currentCycle.LastReturnDate.AddDays(1); // يبدأ من اليوم التالي للعودة
        int dutyDayNum = (today.DayNumber - firstDutyDay.DayNumber) + 1;

        string description = (dutyDayNum >= 1 && dutyDayNum <= soldierType.DutyDays)
            ? $"في الخدمة (اليوم {dutyDayNum} من {soldierType.DutyDays})"
            : "متواجد بالخدمة بالمعسكر";

        return new StatusEvaluationResult(
            SoldierDutyStatus.OnDuty,
            "متواجد بالخدمة",
            description,
            dutyDayNum > 0 ? dutyDayNum : null,
            remainingDaysToLeave
        );
    }
}
