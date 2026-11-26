namespace Boing.Models;

public sealed class WarehouseCardRow
{
    public string? MovementId { get; set; } = null;
    public bool IsPurchase { get; set; }
    public DateOnly Date { get; set; }
    public string Concept { get; set; } = string.Empty;
    public string? ProductionOrderId { get; set; } = null;
    public decimal EntryQuantity { get; set; }
    public decimal ExitQuantity { get; set; }
    public decimal BalanceQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal AverageCost { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal MonetaryBalance { get; set; }
}