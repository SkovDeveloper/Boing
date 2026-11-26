namespace Boing.Models;

public sealed class Material
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public MaterialClassification Classification { get; set; } = MaterialClassification.Direct;
    public decimal InitialQuantity { get; set; }
    public decimal InitialUnitCost { get; set; }
}