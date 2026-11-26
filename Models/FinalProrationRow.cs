namespace Boing.Models;

public sealed class FinalProrationRow
{
    public string ProductionOrderId { get; set; } = string.Empty;
    public string ProductionOrderCode { get; set; } = string.Empty;
    public decimal DirectLaborHours { get; set; }
    public decimal AppliedOverhead { get; set; }
}