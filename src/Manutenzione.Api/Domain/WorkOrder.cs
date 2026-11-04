namespace Manutenzione.Api.Domain;

/// <summary>Ordine di lavoro su una macchina, con un ciclo di vita a stati controllati.</summary>
public class WorkOrder
{
    private static readonly Dictionary<WorkOrderStatus, WorkOrderStatus[]> AllowedTransitions = new()
    {
        [WorkOrderStatus.Open] = [WorkOrderStatus.InProgress, WorkOrderStatus.Cancelled],
        [WorkOrderStatus.InProgress] = [WorkOrderStatus.Completed, WorkOrderStatus.Cancelled],
        [WorkOrderStatus.Completed] = [],
        [WorkOrderStatus.Cancelled] = []
    };

    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }
    public int? TechnicianId { get; set; }
    public Technician? Technician { get; set; }
    public int? PlanId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;
    public WorkOrderStatus Status { get; private set; } = WorkOrderStatus.Open;
    public DateOnly DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; private set; }
    public string? CompletionNotes { get; private set; }

    public bool IsOverdue(DateOnly today) =>
        DueDate < today && Status is WorkOrderStatus.Open or WorkOrderStatus.InProgress;

    public bool CanMoveTo(WorkOrderStatus next) => AllowedTransitions[Status].Contains(next);

    /// <summary>Porta l'ordine nello stato richiesto se la transizione è ammessa.</summary>
    /// <exception cref="InvalidOperationException">Transizione non ammessa dallo stato corrente.</exception>
    public void MoveTo(WorkOrderStatus next, DateTime nowUtc, string? notes = null)
    {
        if (!CanMoveTo(next))
        {
            throw new InvalidOperationException($"Transizione da {Status} a {next} non ammessa per l'ordine {Id}.");
        }

        if (next == WorkOrderStatus.InProgress && TechnicianId is null)
        {
            throw new InvalidOperationException($"L'ordine {Id} deve avere un tecnico assegnato prima di essere avviato.");
        }

        Status = next;

        if (next == WorkOrderStatus.Completed)
        {
            CompletedAt = nowUtc;
            CompletionNotes = notes;
        }
    }
}
