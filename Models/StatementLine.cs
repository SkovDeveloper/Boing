namespace Boing.Models;

public sealed class StatementLine
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsHighlight { get; set; }
    public bool IsSubtraction { get; set; }
}