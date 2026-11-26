namespace Boing.Interfaces;

public interface IProrationEngine
{
    ProrationResult CalculateProrations(
        IEnumerable<Department> departments,
        IEnumerable<OverheadExpense> expenses,
        IEnumerable<ProductionOrder> orders,
        IEnumerable<WarehouseCardResult> warehouseCards
    );
}