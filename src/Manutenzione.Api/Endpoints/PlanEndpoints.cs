using Manutenzione.Api.Contracts;
using Manutenzione.Api.Data;
using Manutenzione.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Manutenzione.Api.Endpoints;

public static class PlanEndpoints
{
    private const int MaxHorizonDays = 365;

    public static RouteGroupBuilder MapPlans(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/plans").WithTags("Piani di manutenzione");

        group.MapGet("/", async (MaintenanceDbContext db, CancellationToken ct) =>
            await db.Plans
                .OrderBy(p => p.NextDueDate)
                .Select(p => new PlanResponse(p.Id, p.AssetId, p.Title, p.IntervalDays, p.Priority, p.NextDueDate, p.Active))
                .ToListAsync(ct));

        group.MapPost("/", async Task<Results<Created<PlanResponse>, NotFound<string>>> (CreatePlanRequest req, MaintenanceDbContext db, CancellationToken ct) =>
        {
            if (!await db.Assets.AnyAsync(a => a.Id == req.AssetId, ct))
            {
                return TypedResults.NotFound($"Macchina {req.AssetId} non trovata.");
            }

            var plan = new MaintenancePlan
            {
                AssetId = req.AssetId,
                Title = req.Title.Trim(),
                IntervalDays = req.IntervalDays,
                Priority = req.Priority,
                NextDueDate = req.FirstDueDate
            };
            db.Plans.Add(plan);
            await db.SaveChangesAsync(ct);
            return TypedResults.Created($"/api/plans/{plan.Id}",
                new PlanResponse(plan.Id, plan.AssetId, plan.Title, plan.IntervalDays, plan.Priority, plan.NextDueDate, plan.Active));
        }).WithValidation<CreatePlanRequest>();

        group.MapPost("/generate", async Task<Results<Ok<GenerateResult>, BadRequest<string>>> (int? daysAhead, MaintenanceDbContext db, TimeProvider clock, CancellationToken ct) =>
        {
            var days = daysAhead ?? 30;
            if (days is < 0 or > MaxHorizonDays)
            {
                return TypedResults.BadRequest($"daysAhead deve essere compreso tra 0 e {MaxHorizonDays}.");
            }

            var horizon = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddDays(days);
            var plans = await db.Plans.Where(p => p.Active && p.NextDueDate <= horizon).ToListAsync(ct);

            var created = 0;
            foreach (var plan in plans)
            {
                foreach (var due in plan.TakeDueDates(horizon))
                {
                    db.WorkOrders.Add(new WorkOrder
                    {
                        AssetId = plan.AssetId,
                        PlanId = plan.Id,
                        Title = plan.Title,
                        Priority = plan.Priority,
                        DueDate = due
                    });
                    created++;
                }
            }

            await db.SaveChangesAsync(ct);
            return TypedResults.Ok(new GenerateResult(created, horizon));
        });

        return group;
    }
}
