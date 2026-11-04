using Manutenzione.Api.Contracts;
using Manutenzione.Api.Data;
using Manutenzione.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Manutenzione.Api.Endpoints;

public static class WorkOrderEndpoints
{
    public static RouteGroupBuilder MapWorkOrders(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/work-orders").WithTags("Ordini di lavoro");

        group.MapGet("/", async (WorkOrderStatus? status, int? assetId, bool? overdue, MaintenanceDbContext db, TimeProvider clock, CancellationToken ct) =>
        {
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            var query = db.WorkOrders.Include(w => w.Asset).Include(w => w.Technician).AsNoTracking();

            if (status is not null)
            {
                query = query.Where(w => w.Status == status);
            }

            if (assetId is not null)
            {
                query = query.Where(w => w.AssetId == assetId);
            }

            if (overdue == true)
            {
                query = query.Where(w => w.DueDate < today && (w.Status == WorkOrderStatus.Open || w.Status == WorkOrderStatus.InProgress));
            }

            var orders = await query.OrderBy(w => w.DueDate).ThenByDescending(w => w.Priority).ToListAsync(ct);
            return orders.Select(w => ToResponse(w, today)).ToList();
        });

        group.MapGet("/{id:int}", async Task<Results<Ok<WorkOrderResponse>, NotFound>> (int id, MaintenanceDbContext db, TimeProvider clock, CancellationToken ct) =>
        {
            var order = await Load(db, id, ct);
            return order is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(order, Today(clock)));
        });

        group.MapPost("/", async Task<Results<Created<WorkOrderResponse>, NotFound<string>>> (CreateWorkOrderRequest req, MaintenanceDbContext db, TimeProvider clock, CancellationToken ct) =>
        {
            var asset = await db.Assets.FindAsync([req.AssetId], ct);
            if (asset is null)
            {
                return TypedResults.NotFound($"Macchina {req.AssetId} non trovata.");
            }

            var order = new WorkOrder
            {
                AssetId = asset.Id,
                Asset = asset,
                Title = req.Title.Trim(),
                Description = req.Description?.Trim(),
                Priority = req.Priority,
                DueDate = req.DueDate
            };
            db.WorkOrders.Add(order);
            await db.SaveChangesAsync(ct);
            return TypedResults.Created($"/api/work-orders/{order.Id}", ToResponse(order, Today(clock)));
        }).WithValidation<CreateWorkOrderRequest>();

        group.MapPut("/{id:int}/technician", async Task<Results<Ok<WorkOrderResponse>, NotFound<string>, Conflict<string>>> (int id, AssignTechnicianRequest req, MaintenanceDbContext db, TimeProvider clock, CancellationToken ct) =>
        {
            var order = await Load(db, id, ct);
            if (order is null)
            {
                return TypedResults.NotFound($"Ordine {id} non trovato.");
            }

            if (order.Status is WorkOrderStatus.Completed or WorkOrderStatus.Cancelled)
            {
                return TypedResults.Conflict($"L'ordine {id} è chiuso e non può essere riassegnato.");
            }

            var technician = await db.Technicians.FirstOrDefaultAsync(t => t.Id == req.TechnicianId && t.Active, ct);
            if (technician is null)
            {
                return TypedResults.NotFound($"Tecnico attivo {req.TechnicianId} non trovato.");
            }

            order.TechnicianId = technician.Id;
            order.Technician = technician;
            await db.SaveChangesAsync(ct);
            return TypedResults.Ok(ToResponse(order, Today(clock)));
        }).WithValidation<AssignTechnicianRequest>();

        group.MapPut("/{id:int}/status", async Task<Results<Ok<WorkOrderResponse>, NotFound<string>, Conflict<string>>> (int id, ChangeStatusRequest req, MaintenanceDbContext db, TimeProvider clock, CancellationToken ct) =>
        {
            var order = await Load(db, id, ct);
            if (order is null)
            {
                return TypedResults.NotFound($"Ordine {id} non trovato.");
            }

            try
            {
                order.MoveTo(req.Status, clock.GetUtcNow().UtcDateTime, req.Notes);
            }
            catch (InvalidOperationException ex)
            {
                return TypedResults.Conflict(ex.Message);
            }

            await db.SaveChangesAsync(ct);
            return TypedResults.Ok(ToResponse(order, Today(clock)));
        }).WithValidation<ChangeStatusRequest>();

        return group;
    }

    private static DateOnly Today(TimeProvider clock) => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

    private static Task<WorkOrder?> Load(MaintenanceDbContext db, int id, CancellationToken ct) =>
        db.WorkOrders.Include(w => w.Asset).Include(w => w.Technician).FirstOrDefaultAsync(w => w.Id == id, ct);

    private static WorkOrderResponse ToResponse(WorkOrder w, DateOnly today) => new(
        w.Id, w.AssetId, w.Asset?.Code ?? string.Empty, w.Title, w.Description, w.Priority, w.Status,
        w.DueDate, w.IsOverdue(today), w.TechnicianId, w.Technician?.FullName, w.PlanId, w.CompletedAt);
}
