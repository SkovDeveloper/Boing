namespace Boing.Models;

public sealed class ProductionOrder
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.InProcess;
    public decimal InitialWorkInProcessCost { get; set; }
    public decimal ProducedUnits { get; set; }
    public decimal SoldUnits { get; set; }
    public List<DirectLaborEntry> DirectLaborEntries { get; set; } = new();
}