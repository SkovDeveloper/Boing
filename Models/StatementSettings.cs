namespace Boing.Models;

public sealed class SignaturePoint
{
    public double X { get; set; }
    public double Y { get; set; }
}

public sealed class StatementSettings
{
    public string CompanyName { get; set; } = "Boing de Mango";
    public DateOnly PeriodStart { get; set; } = new(2026, 11, 1);
    public DateOnly PeriodEnd { get; set; } = new(2026, 11, 30);
    public string PreparedBy { get; set; } = "C.P. Ariel Morales González";
    public string AuthorizedBy { get; set; } = "Dirección General";

    public decimal InitialFinishedGoodsCost { get; set; } = 0m;
    public decimal SellingExpenses { get; set; } = 18500m;
    public decimal AdminExpenses { get; set; } = 22000m;

    public List<List<SignaturePoint>> PreparedSignatureStrokes { get; set; } = [];
    public List<List<SignaturePoint>> AuthorizedSignatureStrokes { get; set; } = [];
}