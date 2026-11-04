namespace Manutenzione.Api.Domain;

public enum WorkOrderStatus
{
    Open,
    InProgress,
    Completed,
    Cancelled
}

public enum WorkOrderPriority
{
    Low,
    Medium,
    High,
    Critical
}
