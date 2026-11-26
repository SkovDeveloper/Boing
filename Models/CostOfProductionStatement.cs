namespace Boing.Models;

public sealed class CostOfProductionStatement
{
    public decimal InitialRawMaterialInventory { get; set; }
    public decimal RawMaterialPurchases { get; set; }
    public decimal AvailableRawMaterial { get; set; }
    public decimal FinalRawMaterialInventory { get; set; }
    public decimal TotalRawMaterialUsed { get; set; }
    public decimal IndirectRawMaterialUsed { get; set; }
    public decimal DirectRawMaterialUsed { get; set; }
    public decimal DirectLaborCost { get; set; }
    public decimal PrimeCost { get; set; }
    public decimal AppliedOverheadCost { get; set; }
    public decimal IncurredProductionCost { get; set; }
    public decimal InitialWorkInProcessInventory { get; set; }
    public decimal TotalProductionInProcess { get; set; }
    public decimal FinalWorkInProcessInventory { get; set; }
    public decimal CostOfFinishedGoods { get; set; }
    public decimal InitialFinishedGoodsInventory { get; set; }
    public decimal AvailableFinishedGoods { get; set; }
    public decimal FinalFinishedGoodsInventory { get; set; }
    public decimal CostOfGoodsSold { get; set; }
}