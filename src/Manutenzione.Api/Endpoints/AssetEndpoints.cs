using Manutenzione.Api.Contracts;
using Manutenzione.Api.Data;
using Manutenzione.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Manutenzione.Api.Endpoints;

public static class AssetEndpoints
{
    public static RouteGroupBuilder MapAssets(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/assets").WithTags("Macchine");

        group.MapGet("/", async (MaintenanceDbContext db, CancellationToken ct) =>
            await db.Assets
                .OrderBy(a => a.Code)
                .Select(a => new AssetResponse(a.Id, a.Code, a.Name, a.Location,
                    a.WorkOrders.Count(w => w.Status == WorkOrderStatus.Open || w.Status == WorkOrderStatus.InProgress)))
                .ToListAsync(ct));

        group.MapGet("/{id:int}", async Task<Results<Ok<AssetResponse>, NotFound>> (int id, MaintenanceDbContext db, CancellationToken ct) =>
        {
            var asset = await db.Assets
                .Where(a => a.Id == id)
                .Select(a => new AssetResponse(a.Id, a.Code, a.Name, a.Location,
                    a.WorkOrders.Count(w => w.Status == WorkOrderStatus.Open || w.Status == WorkOrderStatus.InProgress)))
                .FirstOrDefaultAsync(ct);
            return asset is null ? TypedResults.NotFound() : TypedResults.Ok(asset);
        });

        group.MapPost("/", async Task<Results<Created<AssetResponse>, Conflict<string>>> (CreateAssetRequest req, MaintenanceDbContext db, CancellationToken ct) =>
        {
            var code = req.Code.Trim().ToUpperInvariant();
            if (await db.Assets.AnyAsync(a => a.Code == code, ct))
            {
                return TypedResults.Conflict($"Esiste già una macchina con codice {code}.");
            }

            var asset = new Asset { Code = code, Name = req.Name.Trim(), Location = req.Location?.Trim() };
            db.Assets.Add(asset);
            await db.SaveChangesAsync(ct);
            return TypedResults.Created($"/api/assets/{asset.Id}", new AssetResponse(asset.Id, asset.Code, asset.Name, asset.Location, 0));
        }).WithValidation<CreateAssetRequest>();

        return group;
    }
}
