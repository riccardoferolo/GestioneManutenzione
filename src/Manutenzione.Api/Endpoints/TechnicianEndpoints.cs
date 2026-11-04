using Manutenzione.Api.Contracts;
using Manutenzione.Api.Data;
using Manutenzione.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Manutenzione.Api.Endpoints;

public static class TechnicianEndpoints
{
    public static RouteGroupBuilder MapTechnicians(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/technicians").WithTags("Tecnici");

        group.MapGet("/", async (MaintenanceDbContext db, CancellationToken ct) =>
            await db.Technicians
                .OrderBy(t => t.FullName)
                .Select(t => new TechnicianResponse(t.Id, t.FullName, t.Specialization, t.Active))
                .ToListAsync(ct));

        group.MapPost("/", async (CreateTechnicianRequest req, MaintenanceDbContext db, CancellationToken ct) =>
        {
            var technician = new Technician { FullName = req.FullName.Trim(), Specialization = req.Specialization?.Trim() };
            db.Technicians.Add(technician);
            await db.SaveChangesAsync(ct);
            return TypedResults.Created($"/api/technicians/{technician.Id}",
                new TechnicianResponse(technician.Id, technician.FullName, technician.Specialization, technician.Active));
        }).WithValidation<CreateTechnicianRequest>();

        return group;
    }
}
