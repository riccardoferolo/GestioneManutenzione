using Manutenzione.Api.Domain;

namespace Manutenzione.Api.Tests;

public class MaintenancePlanTests
{
    [Fact]
    public void Inactive_plan_produces_no_due_dates()
    {
        var plan = new MaintenancePlan { Title = "Test", IntervalDays = 10, NextDueDate = new DateOnly(2026, 1, 1), Active = false };

        Assert.Empty(plan.TakeDueDates(new DateOnly(2026, 12, 31)));
    }

    [Fact]
    public void Due_dates_advance_the_next_due_date_past_the_horizon()
    {
        var plan = new MaintenancePlan { Title = "Test", IntervalDays = 30, NextDueDate = new DateOnly(2026, 1, 1) };

        var dates = plan.TakeDueDates(new DateOnly(2026, 2, 15));

        Assert.Equal([new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)], dates);
        Assert.Equal(new DateOnly(2026, 3, 2), plan.NextDueDate);
    }
}
