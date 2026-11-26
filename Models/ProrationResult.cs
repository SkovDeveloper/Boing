namespace Boing.Models;

public sealed class ProrationResult
{
    public List<PrimaryProrationRow> PrimaryRows { get; set; } = new();
    public Dictionary<string, decimal> PrimaryTotalsByDepartment { get; set; } = new();
    public List<SecondaryProrationRow> SecondaryRows { get; set; } = new();
    public Dictionary<string, decimal> SecondaryTotalsByDepartment { get; set; } = new();
    public List<FinalProrationRow> FinalRows { get; set; } = new();
    public decimal TotalOverheadApplied { get; set; }
}