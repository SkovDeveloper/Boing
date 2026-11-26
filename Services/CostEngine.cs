namespace Boing.Services;

public sealed class CostEngine : ICostEngine
{
    private readonly IWarehouseEngine _warehouseEngine;
    private readonly IProrationEngine _prorationEngine;

    public CostEngine(IWarehouseEngine warehouseEngine, IProrationEngine prorationEngine)
    {
        _warehouseEngine = warehouseEngine;
        _prorationEngine = prorationEngine;
    }

    public CostCalculationResult CalculateAll(CostProject project)
    {
        var result = new CostCalculationResult();

        foreach (var item in project.Materials)
        {
            var card = _warehouseEngine.CalculateCard(
                item,
                project.Purchases,
                project.Consumptions,
                project.ValuationMethod);
            result.WarehouseCards.Add(card);
        }

        result.Proration = _prorationEngine.CalculateProrations(
            project.Departments,
            project.OverheadExpenses,
            project.ProductionOrders,
            result.WarehouseCards);

        var departmentLookup = project.Departments.ToDictionary(d => d.Id);
        var appliedOverheadLookup = result.Proration.FinalRows
            .ToDictionary(r => r.ProductionOrderId, r => r.AppliedOverhead);

        foreach (var order in project.ProductionOrders)
        {
            var directMaterialCost = result.WarehouseCards
                .Where(c => c.Classification == MaterialClassification.Direct)
                .Sum(c => c.CostByOrder.GetValueOrDefault(order.Id, 0m));

            var directLaborCost = order.DirectLaborEntries.Sum(entry =>
            {
                var rate = departmentLookup.TryGetValue(entry.DepartmentId, out var dept)
                    ? dept.DirectLaborHourlyRate ?? 0m
                    : 0m;
                return entry.Hours * rate;
            });

            var appliedOverheadCost = appliedOverheadLookup.GetValueOrDefault(order.Id, 0m);
            var totalProductionCost = order.InitialWorkInProcessCost + directMaterialCost + directLaborCost + appliedOverheadCost;
            var unitProductionCost = order.ProducedUnits > 0m ? totalProductionCost / order.ProducedUnits : 0m;

            var unitSalePrice = project.PricingMode == PricingMode.MarkupPercentage
                ? unitProductionCost * (1m + (project.PricingValue / 100m))
                : project.PricingValue;

            var costOfGoodsSold = order.SoldUnits * unitProductionCost;
            var totalSalesRevenue = order.SoldUnits * unitSalePrice;

            result.OrderSummaries.Add(new OrderCostSummary
            {
                ProductionOrderId = order.Id,
                OrderCode = order.Code,
                Status = order.Status,
                InitialWorkInProcessCost = order.InitialWorkInProcessCost,
                DirectMaterialCost = directMaterialCost,
                DirectLaborCost = directLaborCost,
                AppliedOverheadCost = appliedOverheadCost,
                TotalProductionCost = totalProductionCost,
                ProducedUnits = order.ProducedUnits,
                UnitProductionCost = unitProductionCost,
                SoldUnits = order.SoldUnits,
                UnitSalePrice = unitSalePrice,
                TotalSalesRevenue = totalSalesRevenue,
                CostOfGoodsSold = costOfGoodsSold
            });
        }

        var initialRawMaterial = result.WarehouseCards.Sum(c => c.InitialInventoryCost);
        var purchasesTotal = result.WarehouseCards.Sum(c => c.TotalPurchasesCost);
        var availableRawMaterial = initialRawMaterial + purchasesTotal;
        var finalRawMaterial = result.WarehouseCards.Sum(c => c.FinalInventoryCost);
        var totalRawMaterialUsed = availableRawMaterial - finalRawMaterial;
        var indirectRawMaterialUsed = result.WarehouseCards
            .Where(c => c.Classification == MaterialClassification.Indirect)
            .Sum(c => c.TotalConsumptionsCost);
        var directRawMaterialUsed = totalRawMaterialUsed - indirectRawMaterialUsed;

        var totalDirectLabor = result.OrderSummaries.Sum(o => o.DirectLaborCost);
        var primeCost = directRawMaterialUsed + totalDirectLabor;
        var appliedOverhead = result.Proration.TotalOverheadApplied;
        var incurredProductionCost = primeCost + appliedOverhead;

        var initialWip = result.OrderSummaries.Sum(o => o.InitialWorkInProcessCost);
        var totalProductionInProcess = incurredProductionCost + initialWip;
        var finalWip = result.OrderSummaries
            .Where(o => o.Status == OrderStatus.InProcess)
            .Sum(o => o.TotalProductionCost);

        var costOfFinishedGoods = totalProductionInProcess - finalWip;
        var initialFinishedGoods = project.InitialFinishedGoodsUnits * project.InitialFinishedGoodsUnitCost;
        var availableFinishedGoods = costOfFinishedGoods + initialFinishedGoods;
        var totalCostOfGoodsSold = result.OrderSummaries.Sum(o => o.CostOfGoodsSold);
        var finalFinishedGoods = availableFinishedGoods - totalCostOfGoodsSold;

        result.CostOfProductionStatement = new CostOfProductionStatement
        {
            InitialRawMaterialInventory = initialRawMaterial,
            RawMaterialPurchases = purchasesTotal,
            AvailableRawMaterial = availableRawMaterial,
            FinalRawMaterialInventory = finalRawMaterial,
            TotalRawMaterialUsed = totalRawMaterialUsed,
            IndirectRawMaterialUsed = indirectRawMaterialUsed,
            DirectRawMaterialUsed = directRawMaterialUsed,
            DirectLaborCost = totalDirectLabor,
            PrimeCost = primeCost,
            AppliedOverheadCost = appliedOverhead,
            IncurredProductionCost = incurredProductionCost,
            InitialWorkInProcessInventory = initialWip,
            TotalProductionInProcess = totalProductionInProcess,
            FinalWorkInProcessInventory = finalWip,
            CostOfFinishedGoods = costOfFinishedGoods,
            InitialFinishedGoodsInventory = initialFinishedGoods,
            AvailableFinishedGoods = availableFinishedGoods,
            FinalFinishedGoodsInventory = finalFinishedGoods,
            CostOfGoodsSold = totalCostOfGoodsSold
        };

        var totalSales = result.OrderSummaries.Sum(o => o.TotalSalesRevenue);
        var grossProfit = totalSales - totalCostOfGoodsSold;
        var totalOperatingExpenses = project.SellingExpenses + project.AdministrativeExpenses;
        var operatingIncome = grossProfit - totalOperatingExpenses;

        result.IncomeStatement = new IncomeStatement
        {
            TotalSales = totalSales,
            CostOfGoodsSold = totalCostOfGoodsSold,
            GrossProfit = grossProfit,
            SellingExpenses = project.SellingExpenses,
            AdministrativeExpenses = project.AdministrativeExpenses,
            TotalOperatingExpenses = totalOperatingExpenses,
            OperatingIncome = operatingIncome
        };

        return result;
    }
}