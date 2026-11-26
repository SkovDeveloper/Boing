namespace Boing.Models;

public sealed class OrderCostSummary
{
    public string ProductionOrderId { get; set; } = string.Empty;
    public string OrderCode { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal InitialWorkInProcessCost { get; set; }
    public decimal DirectMaterialCost { get; set; }
    public decimal DirectLaborCost { get; set; }
    public decimal AppliedOverheadCost { get; set; }
    public decimal TotalProductionCost { get; set; }
    public decimal ProducedUnits { get; set; }
    public decimal UnitProductionCost { get; set; }
    public decimal SoldUnits { get; set; }
    public decimal UnitSalePrice { get; set; }
    public decimal TotalSalesRevenue { get; set; }
    public decimal CostOfGoodsSold { get; set; }
}