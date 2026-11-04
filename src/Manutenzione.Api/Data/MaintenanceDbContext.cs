using Manutenzione.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Manutenzione.Api.Data;

public class MaintenanceDbContext(DbContextOptions<MaintenanceDbContext> options) : DbContext(options)
{
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<MaintenancePlan> Plans => Set<MaintenancePlan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Asset>(e =>
        {
            e.Property(a => a.Code).HasMaxLength(32);
            e.Property(a => a.Name).HasMaxLength(200);
            e.Property(a => a.Location).HasMaxLength(200);
            e.HasIndex(a => a.Code).IsUnique();
        });

        modelBuilder.Entity<Technician>(e =>
        {
            e.Property(t => t.FullName).HasMaxLength(150);
            e.Property(t => t.Specialization).HasMaxLength(100);
        });

        modelBuilder.Entity<WorkOrder>(e =>
        {
            e.Property(w => w.Title).HasMaxLength(200);
            e.Property(w => w.Description).HasMaxLength(2000);
            e.Property(w => w.CompletionNotes).HasMaxLength(2000);
            e.Property(w => w.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(w => w.Priority).HasConversion<string>().HasMaxLength(20);
            e.HasOne(w => w.Asset).WithMany(a => a.WorkOrders).HasForeignKey(w => w.AssetId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(w => w.Technician).WithMany().HasForeignKey(w => w.TechnicianId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(w => new { w.Status, w.DueDate });
            e.HasIndex(w => new { w.PlanId, w.DueDate }).IsUnique().HasFilter("[PlanId] IS NOT NULL");
        });

        modelBuilder.Entity<MaintenancePlan>(e =>
        {
            e.Property(p => p.Title).HasMaxLength(200);
            e.Property(p => p.Priority).HasConversion<string>().HasMaxLength(20);
            e.HasOne(p => p.Asset).WithMany(a => a.Plans).HasForeignKey(p => p.AssetId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
