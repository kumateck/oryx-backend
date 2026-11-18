namespace DOMAIN.Entities.Materials;

/// <summary>Append-only record of a shared material density change, in g/mL.</summary>
public sealed class MaterialDensityMeasurement
{
    public Guid Id { get; set; }
    public Guid MaterialId { get; set; }
    public Guid ActorId { get; set; }
    public DateTime RecordedAt { get; set; }
    public decimal? PreviousDensity { get; set; }
    public decimal Density { get; set; }
}
