namespace Manutenzione.Api.Domain;

/// <summary>Tecnico a cui vengono assegnati gli ordini di lavoro.</summary>
public class Technician
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public string? Specialization { get; set; }
    public bool Active { get; set; } = true;
}
