namespace Boing.Models;

public sealed class SecondaryProrationRow
{
    public string SourceDepartmentId { get; set; } = string.Empty;
    public string SourceDepartmentName { get; set; } = string.Empty;
    public decimal TotalDistributed { get; set; }
    public string AllocationBase { get; set; } = string.Empty;
    public Dictionary<string, decimal> AmountsByDepartment { get; set; } = new();
}