namespace Boing.Models;

public sealed class PrimaryProrationRow
{
    public string ExpenseId { get; set; } = string.Empty;
    public string Concept { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string AllocationBase { get; set; } = string.Empty;
    public Dictionary<string, decimal> AmountsByDepartment { get; set; } = new();
}