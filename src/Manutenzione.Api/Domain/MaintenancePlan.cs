namespace Manutenzione.Api.Domain;

/// <summary>Piano di manutenzione preventiva che genera ordini di lavoro a intervalli regolari.</summary>
public class MaintenancePlan
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }
    public required string Title { get; set; }
    public int IntervalDays { get; set; }
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;
    public DateOnly NextDueDate { get; set; }
    public bool Active { get; set; } = true;

    /// <summary>Restituisce le scadenze maturate fino a <paramref name="horizon"/> e avanza la prossima scadenza.</summary>
    public IReadOnlyList<DateOnly> TakeDueDates(DateOnly horizon)
    {
        if (!Active || IntervalDays <= 0)
        {
            return [];
        }

        var dates = new List<DateOnly>();
        while (NextDueDate <= horizon)
        {
            dates.Add(NextDueDate);
            NextDueDate = NextDueDate.AddDays(IntervalDays);
        }

        return dates;
    }
}
