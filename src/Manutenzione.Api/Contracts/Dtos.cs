using System.ComponentModel.DataAnnotations;
using Manutenzione.Api.Domain;

namespace Manutenzione.Api.Contracts;

public record CreateAssetRequest(
    [property: Required, StringLength(32, MinimumLength = 1)] string Code,
    [property: Required, StringLength(200, MinimumLength = 1)] string Name,
    [property: StringLength(200)] string? Location);

public record AssetResponse(int Id, string Code, string Name, string? Location, int OpenWorkOrders);

public record CreateTechnicianRequest(
    [property: Required, StringLength(150, MinimumLength = 1)] string FullName,
    [property: StringLength(100)] string? Specialization);

public record TechnicianResponse(int Id, string FullName, string? Specialization, bool Active);

public record CreateWorkOrderRequest(
    [property: Range(1, int.MaxValue)] int AssetId,
    [property: Required, StringLength(200, MinimumLength = 1)] string Title,
    [property: StringLength(2000)] string? Description,
    WorkOrderPriority Priority,
    DateOnly DueDate);

public record AssignTechnicianRequest([property: Range(1, int.MaxValue)] int TechnicianId);

public record ChangeStatusRequest(WorkOrderStatus Status, [property: StringLength(2000)] string? Notes);

public record WorkOrderResponse(
    int Id,
    int AssetId,
    string AssetCode,
    string Title,
    string? Description,
    WorkOrderPriority Priority,
    WorkOrderStatus Status,
    DateOnly DueDate,
    bool Overdue,
    int? TechnicianId,
    string? TechnicianName,
    int? PlanId,
    DateTime? CompletedAt);

public record CreatePlanRequest(
    [property: Range(1, int.MaxValue)] int AssetId,
    [property: Required, StringLength(200, MinimumLength = 1)] string Title,
    [property: Range(1, 3650)] int IntervalDays,
    WorkOrderPriority Priority,
    DateOnly FirstDueDate);

public record PlanResponse(int Id, int AssetId, string Title, int IntervalDays, WorkOrderPriority Priority, DateOnly NextDueDate, bool Active);

public record GenerateResult(int Created, DateOnly Horizon);
