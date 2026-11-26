namespace Boing.Interfaces;

public interface IOrderCodeGeneratorService
{
    string GenerateNextCode(IEnumerable<ProductionOrder> existingOrders);
}