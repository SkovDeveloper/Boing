namespace Boing.Models;

public sealed class OverheadExpense
{
    public string Id { get; set; } = string.Empty;
    public string Concept { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public AllocationType AllocationType { get; set; }
    public string? TargetDepartmentId { get; set; } = null;
    public string? AllocationBaseKey { get; set; } = null;
}