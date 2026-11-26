namespace Boing.Services;

public sealed class ProrationEngine : IProrationEngine
{
    public ProrationResult CalculateProrations(
        IEnumerable<Department> departments,
        IEnumerable<OverheadExpense> expenses,
        IEnumerable<ProductionOrder> orders,
        IEnumerable<WarehouseCardResult> warehouseCards)
    {
        var result = new ProrationResult();
        var departmentList = departments.ToList();
        var orderList = orders.ToList();

        foreach (var dept in departmentList)
        {
            result.PrimaryTotalsByDepartment[dept.Id] = 0m;
        }

        var allExpenses = expenses.ToList();
        var indirectMaterialTotal = warehouseCards
            .Where(c => c.Classification == MaterialClassification.Indirect)
            .Sum(c => c.TotalConsumptionsCost);

        if (indirectMaterialTotal > 0m)
        {
            allExpenses.Insert(0, new OverheadExpense
            {
                Id = "SYS-INDIRECT-MP",
                Concept = "Indirect Raw Material",
                Amount = indirectMaterialTotal,
                AllocationType = AllocationType.MetricBased,
                AllocationBaseKey = "IndirectMaterialBase"
            });
        }

        foreach (var expense in allExpenses)
        {
            var row = new PrimaryProrationRow
            {
                ExpenseId = expense.Id,
                Concept = expense.Concept,
                TotalAmount = expense.Amount,
                AllocationBase = expense.AllocationType == AllocationType.DirectAssignment
                    ? "Direct Assignment"
                    : expense.AllocationBaseKey ?? "Equal"
            };

            if (expense.AllocationType == AllocationType.DirectAssignment &&
                !string.IsNullOrWhiteSpace(expense.TargetDepartmentId) &&
                result.PrimaryTotalsByDepartment.ContainsKey(expense.TargetDepartmentId))
            {
                foreach (var dept in departmentList)
                {
                    var assigned = dept.Id == expense.TargetDepartmentId ? expense.Amount : 0m;
                    row.AmountsByDepartment[dept.Id] = assigned;
                    result.PrimaryTotalsByDepartment[dept.Id] += assigned;
                }
            }
            else
            {
                var key = expense.AllocationBaseKey ?? string.Empty;
                var totalMetric = departmentList.Sum(d => d.Metrics.GetValueOrDefault(key, 0m));

                if (totalMetric > 0m)
                {
                    foreach (var dept in departmentList)
                    {
                        var metric = dept.Metrics.GetValueOrDefault(key, 0m);
                        var assigned = expense.Amount * (metric / totalMetric);
                        row.AmountsByDepartment[dept.Id] = assigned;
                        result.PrimaryTotalsByDepartment[dept.Id] += assigned;
                    }
                }
                else
                {
                    var productiveDepts = departmentList
                        .Where(d => d.Type == DepartmentType.Productive)
                        .ToList();
                    var split = productiveDepts.Count > 0 ? expense.Amount / productiveDepts.Count : 0m;

                    foreach (var dept in departmentList)
                    {
                        var assigned = dept.Type == DepartmentType.Productive ? split : 0m;
                        row.AmountsByDepartment[dept.Id] = assigned;
                        result.PrimaryTotalsByDepartment[dept.Id] += assigned;
                    }
                }
            }

            result.PrimaryRows.Add(row);
        }

        var runningTotals = new Dictionary<string, decimal>(result.PrimaryTotalsByDepartment);
        var serviceDepts = departmentList
            .Where(d => d.Type == DepartmentType.Service)
            .OrderBy(d => d.SecondaryCloseOrder ?? int.MaxValue)
            .ToList();

        var openDepartments = departmentList.Select(d => d.Id).ToHashSet();

        foreach (var serviceDept in serviceDepts)
        {
            openDepartments.Remove(serviceDept.Id);
            var amountToDistribute = runningTotals.GetValueOrDefault(serviceDept.Id, 0m);
            runningTotals[serviceDept.Id] = 0m;

            var baseKey = serviceDept.SecondaryAllocationBaseKey ?? string.Empty;
            var eligibleTargets = departmentList
                .Where(d => openDepartments.Contains(d.Id))
                .ToList();

            var row = new SecondaryProrationRow
            {
                SourceDepartmentId = serviceDept.Id,
                SourceDepartmentName = serviceDept.Name,
                TotalDistributed = amountToDistribute,
                AllocationBase = string.IsNullOrWhiteSpace(baseKey) ? "Equal" : baseKey
            };

            var totalWeight = eligibleTargets.Sum(d => d.Metrics.GetValueOrDefault(baseKey, 0m));

            if (totalWeight > 0m)
            {
                foreach (var target in eligibleTargets)
                {
                    var weight = target.Metrics.GetValueOrDefault(baseKey, 0m);
                    var assigned = amountToDistribute * (weight / totalWeight);
                    row.AmountsByDepartment[target.Id] = assigned;
                    runningTotals[target.Id] += assigned;
                }
            }
            else
            {
                var productiveTargets = eligibleTargets
                    .Where(d => d.Type == DepartmentType.Productive)
                    .ToList();
                var split = productiveTargets.Count > 0 ? amountToDistribute / productiveTargets.Count : 0m;

                foreach (var target in productiveTargets)
                {
                    row.AmountsByDepartment[target.Id] = split;
                    runningTotals[target.Id] += split;
                }
            }

            result.SecondaryRows.Add(row);
        }

        result.SecondaryTotalsByDepartment = runningTotals;

        var appliedByOrder = orderList.ToDictionary(o => o.Id, _ => 0m);
        var productiveList = departmentList
            .Where(d => d.Type == DepartmentType.Productive)
            .ToList();

        foreach (var prodDept in productiveList)
        {
            var deptOverhead = runningTotals.GetValueOrDefault(prodDept.Id, 0m);
            var totalDeptHours = orderList.Sum(o =>
                o.DirectLaborEntries
                    .Where(e => e.DepartmentId == prodDept.Id)
                    .Sum(e => e.Hours));

            if (totalDeptHours > 0m)
            {
                foreach (var order in orderList)
                {
                    var orderDeptHours = order.DirectLaborEntries
                        .Where(e => e.DepartmentId == prodDept.Id)
                        .Sum(e => e.Hours);
                    appliedByOrder[order.Id] += deptOverhead * (orderDeptHours / totalDeptHours);
                }
            }
            else
            {
                var globalHours = orderList.Sum(o => o.DirectLaborEntries.Sum(e => e.Hours));
                if (globalHours > 0m)
                {
                    foreach (var order in orderList)
                    {
                        var orderGlobalHours = order.DirectLaborEntries.Sum(e => e.Hours);
                        appliedByOrder[order.Id] += deptOverhead * (orderGlobalHours / globalHours);
                    }
                }
            }
        }

        foreach (var order in orderList)
        {
            var orderHours = order.DirectLaborEntries.Sum(e => e.Hours);
            var applied = appliedByOrder.GetValueOrDefault(order.Id, 0m);

            result.FinalRows.Add(new FinalProrationRow
            {
                ProductionOrderId = order.Id,
                ProductionOrderCode = order.Code,
                DirectLaborHours = orderHours,
                AppliedOverhead = applied
            });
        }

        result.TotalOverheadApplied = result.FinalRows.Sum(r => r.AppliedOverhead);
        return result;
    }
}