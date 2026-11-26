using Boing.Models;

namespace Boing.Interfaces;

public interface IWarehouseEngine
{
    WarehouseCardResult CalculateCard(
        Material material,
        IEnumerable<Purchase> purchases,
        IEnumerable<Consumption> consumptions,
        ValuationMethod method
    );
}