namespace Boing.Models;

public sealed class Department
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DepartmentType Type { get; set; }
    public decimal? DirectLaborHourlyRate { get; set; } = null;
    public int? SecondaryCloseOrder { get; set; } = null;
    public string? SecondaryAllocationBaseKey { get; set; } = null;
    public Dictionary<string, decimal> Metrics { get; set; } = new();
}