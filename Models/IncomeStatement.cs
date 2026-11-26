namespace Boing.Models;

public sealed class IncomeStatement
{
    public decimal TotalSales { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal SellingExpenses { get; set; }
    public decimal AdministrativeExpenses { get; set; }
    public decimal TotalOperatingExpenses { get; set; }
    public decimal OperatingIncome { get; set; }
}