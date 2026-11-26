namespace Boing.Models;

public sealed class CostProject
{
    public StatementSettings StatementSettings { get; set; } = new();
    
    public string ProductName { get; set; } = string.Empty;
    public string PeriodName { get; set; } = string.Empty;
    public ValuationMethod ValuationMethod { get; set; } = ValuationMethod.Fifo;
    public PricingMode PricingMode { get; set; } = PricingMode.MarkupPercentage;
    public decimal PricingValue { get; set; }
    public decimal InitialFinishedGoodsUnits { get; set; }
    public decimal InitialFinishedGoodsUnitCost { get; set; }
    public decimal SellingExpenses { get; set; }
    public decimal AdministrativeExpenses { get; set; }
    public List<Material> Materials { get; set; } = new();
    public List<Purchase> Purchases { get; set; } = new();
    public List<Consumption> Consumptions { get; set; } = new();
    public List<Department> Departments { get; set; } = new();
    public List<OverheadExpense> OverheadExpenses { get; set; } = new();
    public List<ProductionOrder> ProductionOrders { get; set; } = new();
}