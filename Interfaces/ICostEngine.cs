namespace Boing.Interfaces;

public interface ICostEngine
{
    CostCalculationResult CalculateAll(CostProject project);
}