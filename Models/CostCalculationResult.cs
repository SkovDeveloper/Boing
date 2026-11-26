namespace Boing.Models;

public sealed class CostCalculationResult
{
    public List<WarehouseCardResult> WarehouseCards { get; set; } = new();
    public ProrationResult Proration { get; set; } = new();
    public List<OrderCostSummary> OrderSummaries { get; set; } = new();
    public CostOfProductionStatement CostOfProductionStatement { get; set; } = new();
    public IncomeStatement IncomeStatement { get; set; } = new();
}