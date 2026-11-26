namespace Boing.Models;

public sealed class WarehouseCardResult
{
    public string MaterialId { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public MaterialClassification Classification { get; set; }
    public List<WarehouseCardRow> Rows { get; set; } = new();
    public decimal InitialInventoryCost { get; set; }
    public decimal TotalPurchasesCost { get; set; }
    public decimal TotalConsumptionsCost { get; set; }
    public decimal FinalInventoryCost { get; set; }
    public Dictionary<string, decimal> CostByOrder { get; set; } = new();
}