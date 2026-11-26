namespace Boing.Services;

public sealed class WarehouseEngine : IWarehouseEngine
{
    public WarehouseCardResult CalculateCard(
        Material material,
        IEnumerable<Purchase> purchases,
        IEnumerable<Consumption> consumptions,
        ValuationMethod method)
    {
        var result = new WarehouseCardResult
        {
            MaterialId = material.Id,
            MaterialName = material.Name,
            Unit = material.Unit,
            Classification = material.Classification
        };

        var balanceQuantity = material.InitialQuantity;
        var monetaryBalance = material.InitialQuantity * material.InitialUnitCost;
        var currentAverage = balanceQuantity > 0m ? monetaryBalance / balanceQuantity : material.InitialUnitCost;

        result.InitialInventoryCost = monetaryBalance;

        List<(decimal Quantity, decimal UnitCost)> layers = [];
        if (material.InitialQuantity > 0m)
        {
            layers.Add((material.InitialQuantity, material.InitialUnitCost));
        }

        result.Rows.Add(new WarehouseCardRow
        {
            MovementId = null,
            IsPurchase = false,
            Date = DateOnly.MinValue,
            Concept = "Saldo inicial",
            ProductionOrderId = null,
            EntryQuantity = material.InitialQuantity,
            ExitQuantity = 0m,
            BalanceQuantity = balanceQuantity,
            UnitCost = material.InitialUnitCost,
            AverageCost = currentAverage,
            Debit = monetaryBalance,
            Credit = 0m,
            MonetaryBalance = monetaryBalance
        });

        List<(DateOnly Date, int Priority, Purchase? Purchase, Consumption? Consumption)> timeline = [];

        foreach (var item in purchases.Where(p => p.MaterialId == material.Id))
        {
            timeline.Add((item.Date, 0, item, null));
        }

        foreach (var item in consumptions.Where(c => c.MaterialId == material.Id))
        {
            timeline.Add((item.Date, 1, null, item));
        }

        foreach (var timelineEvent in timeline.OrderBy(e => e.Date).ThenBy(e => e.Priority))
        {
            if (timelineEvent.Purchase is not null)
            {
                var currentPurchase = timelineEvent.Purchase;
                var debit = currentPurchase.Quantity * currentPurchase.UnitCost;
                balanceQuantity += currentPurchase.Quantity;
                monetaryBalance += debit;
                currentAverage = balanceQuantity > 0m ? monetaryBalance / balanceQuantity : 0m;
                result.TotalPurchasesCost += debit;

                if (currentPurchase.Quantity > 0m)
                {
                    layers.Add((currentPurchase.Quantity, currentPurchase.UnitCost));
                }

                result.Rows.Add(new WarehouseCardRow
                {
                    MovementId = currentPurchase.Id,
                    IsPurchase = true,
                    Date = currentPurchase.Date,
                    Concept = "Compra",
                    ProductionOrderId = null,
                    EntryQuantity = currentPurchase.Quantity,
                    ExitQuantity = 0m,
                    BalanceQuantity = balanceQuantity,
                    UnitCost = currentPurchase.UnitCost,
                    AverageCost = currentAverage,
                    Debit = debit,
                    Credit = 0m,
                    MonetaryBalance = monetaryBalance
                });
            }
            else if (timelineEvent.Consumption is not null)
            {
                var currentConsumption = timelineEvent.Consumption;
                if (method == ValuationMethod.WeightedAverage)
                {
                    var exitUnitCost = balanceQuantity > 0m ? monetaryBalance / balanceQuantity : 0m;
                    var credit = currentConsumption.Quantity * exitUnitCost;
                    balanceQuantity -= currentConsumption.Quantity;
                    monetaryBalance -= credit;
                    currentAverage = balanceQuantity > 0m ? monetaryBalance / balanceQuantity : 0m;
                    result.TotalConsumptionsCost += credit;

                    AddOrderCost(result.CostByOrder, currentConsumption.ProductionOrderId, credit);

                    result.Rows.Add(new WarehouseCardRow
                    {
                        MovementId = currentConsumption.Id,
                        IsPurchase = false,
                        Date = currentConsumption.Date,
                        Concept = "Consumo",
                        ProductionOrderId = currentConsumption.ProductionOrderId,
                        EntryQuantity = 0m,
                        ExitQuantity = currentConsumption.Quantity,
                        BalanceQuantity = balanceQuantity,
                        UnitCost = exitUnitCost,
                        AverageCost = currentAverage,
                        Debit = 0m,
                        Credit = credit,
                        MonetaryBalance = monetaryBalance
                    });
                }
                else
                {
                    var remaining = currentConsumption.Quantity;
                    const string baseConcept = "Consumo";

                    List<WarehouseCardRow> splitRows = [];

                    while (remaining > 0m && layers.Count > 0)
                    {
                        var layerIndex = method == ValuationMethod.Fifo ? 0 : layers.Count - 1;
                        var layer = layers[layerIndex];
                        var taken = Math.Min(remaining, layer.Quantity);
                        var credit = taken * layer.UnitCost;

                        if (taken == layer.Quantity)
                        {
                            layers.RemoveAt(layerIndex);
                        }
                        else
                        {
                            layers[layerIndex] = (layer.Quantity - taken, layer.UnitCost);
                        }

                        remaining -= taken;
                        balanceQuantity -= taken;
                        monetaryBalance -= credit;
                        currentAverage = balanceQuantity > 0m ? monetaryBalance / balanceQuantity : 0m;
                        result.TotalConsumptionsCost += credit;

                        AddOrderCost(result.CostByOrder, currentConsumption.ProductionOrderId, credit);

                        splitRows.Add(new WarehouseCardRow
                        {
                            MovementId = currentConsumption.Id,
                            IsPurchase = false,
                            Date = currentConsumption.Date,
                            Concept = baseConcept,
                            ProductionOrderId = currentConsumption.ProductionOrderId,
                            EntryQuantity = 0m,
                            ExitQuantity = taken,
                            BalanceQuantity = balanceQuantity,
                            UnitCost = layer.UnitCost,
                            AverageCost = currentAverage,
                            Debit = 0m,
                            Credit = credit,
                            MonetaryBalance = monetaryBalance
                        });
                    }

                    if (remaining > 0m)
                    {
                        balanceQuantity -= remaining;
                        splitRows.Add(new WarehouseCardRow
                        {
                            MovementId = currentConsumption.Id,
                            IsPurchase = false,
                            Date = currentConsumption.Date,
                            Concept = $"{baseConcept} · Faltante sin existencia",
                            ProductionOrderId = currentConsumption.ProductionOrderId,
                            EntryQuantity = 0m,
                            ExitQuantity = remaining,
                            BalanceQuantity = balanceQuantity,
                            UnitCost = 0m,
                            AverageCost = currentAverage,
                            Debit = 0m,
                            Credit = 0m,
                            MonetaryBalance = monetaryBalance
                        });
                    }

                    if (splitRows.Count > 1)
                    {
                        for (var index = 0; index < splitRows.Count; index++)
                        {
                            if (!splitRows[index].Concept.Contains("Faltante", StringComparison.Ordinal))
                            {
                                splitRows[index].Concept = $"{baseConcept} · Parte {index + 1}/{splitRows.Count}";
                            }
                        }
                    }

                    result.Rows.AddRange(splitRows);
                }
            }
        }

        result.FinalInventoryCost = monetaryBalance;
        return result;
    }

    private static void AddOrderCost(Dictionary<string, decimal> costByOrder, string orderId, decimal cost) =>
        costByOrder[orderId] = costByOrder.GetValueOrDefault(orderId) + cost;
}