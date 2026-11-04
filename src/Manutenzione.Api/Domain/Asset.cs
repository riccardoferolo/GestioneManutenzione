namespace Manutenzione.Api.Domain;

/// <summary>Macchina o impianto soggetto a manutenzione.</summary>
public class Asset
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<WorkOrder> WorkOrders { get; set; } = [];
    public List<MaintenancePlan> Plans { get; set; } = [];
}
