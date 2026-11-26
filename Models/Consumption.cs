namespace Boing.Models;

public sealed class Consumption
{
    public string Id { get; set; } = string.Empty;
    public string MaterialId { get; set; } = string.Empty;
    public string ProductionOrderId { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public decimal Quantity { get; set; }
}