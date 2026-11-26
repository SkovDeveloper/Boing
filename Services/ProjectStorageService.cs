using System.Text.Json;

namespace Boing.Services;

public sealed class ProjectStorageService : IProjectStorageService
{
    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        WriteIndented = true
    };

    public Task<CostProject> LoadDefaultScenarioAsync()
    {
        var project = new CostProject
        {
            ProductName = "Boing de Mango 250 ml",
            PeriodName = "Noviembre 2026",
            ValuationMethod = ValuationMethod.Fifo,
            PricingMode = PricingMode.MarkupPercentage,
            PricingValue = 60m,
            InitialFinishedGoodsUnits = 0m,
            InitialFinishedGoodsUnitCost = 0m,
            SellingExpenses = 18500m,
            AdministrativeExpenses = 14200m,
            Materials =
            [
                new Material { Id = "MAT-01", Name = "Pulpa de mango", Unit = "kg", Classification = MaterialClassification.Direct, InitialQuantity = 300m, InitialUnitCost = 34m },
                new Material { Id = "MAT-02", Name = "Azúcar estándar", Unit = "kg", Classification = MaterialClassification.Direct, InitialQuantity = 200m, InitialUnitCost = 22m },
                new Material { Id = "MAT-03", Name = "Agua purificada", Unit = "L", Classification = MaterialClassification.Direct, InitialQuantity = 1000m, InitialUnitCost = 1.50m },
                new Material { Id = "MAT-04", Name = "Ácido cítrico", Unit = "kg", Classification = MaterialClassification.Direct, InitialQuantity = 25m, InitialUnitCost = 95m },
                new Material { Id = "MAT-05", Name = "Envase Tetra Pak 250 ml", Unit = "pzas", Classification = MaterialClassification.Direct, InitialQuantity = 4000m, InitialUnitCost = 1.80m },
                new Material { Id = "MAT-06", Name = "Popote", Unit = "pzas", Classification = MaterialClassification.Direct, InitialQuantity = 4000m, InitialUnitCost = 0.25m },
                new Material { Id = "MAT-07", Name = "Caja corrugada (24 pzas)", Unit = "pzas", Classification = MaterialClassification.Indirect, InitialQuantity = 200m, InitialUnitCost = 12m }
            ],
            Purchases =
            [
                new Purchase { Id = "PUR-01", MaterialId = "MAT-01", Date = new DateOnly(2026, 11, 3), Quantity = 800m, UnitCost = 36m },
                new Purchase { Id = "PUR-02", MaterialId = "MAT-02", Date = new DateOnly(2026, 11, 3), Quantity = 500m, UnitCost = 23.50m },
                new Purchase { Id = "PUR-03", MaterialId = "MAT-03", Date = new DateOnly(2026, 11, 3), Quantity = 3000m, UnitCost = 1.60m },
                new Purchase { Id = "PUR-04", MaterialId = "MAT-04", Date = new DateOnly(2026, 11, 4), Quantity = 40m, UnitCost = 98m },
                new Purchase { Id = "PUR-05", MaterialId = "MAT-05", Date = new DateOnly(2026, 11, 4), Quantity = 12000m, UnitCost = 1.90m },
                new Purchase { Id = "PUR-06", MaterialId = "MAT-06", Date = new DateOnly(2026, 11, 4), Quantity = 12000m, UnitCost = 0.28m },
                new Purchase { Id = "PUR-07", MaterialId = "MAT-07", Date = new DateOnly(2026, 11, 5), Quantity = 500m, UnitCost = 12.50m }
            ],
            Departments =
            [
                new Department
                {
                    Id = "DEP-P1",
                    Name = "Mezclado y Pasteurización",
                    Type = DepartmentType.Productive,
                    DirectLaborHourlyRate = 85m,
                    Metrics = new()
                    {
                        ["Metros cuadrados"] = 120m,
                        ["Kilowatts"] = 90m,
                        ["Horas-máquina"] = 420m,
                        ["Materia prima"] = 60m,
                        ["IndirectMaterialBase"] = 50m
                    }
                },
                new Department
                {
                    Id = "DEP-P2",
                    Name = "Envasado",
                    Type = DepartmentType.Productive,
                    DirectLaborHourlyRate = 78m,
                    Metrics = new()
                    {
                        ["Metros cuadrados"] = 100m,
                        ["Kilowatts"] = 70m,
                        ["Horas-máquina"] = 380m,
                        ["Materia prima"] = 40m,
                        ["IndirectMaterialBase"] = 50m
                    }
                },
                new Department
                {
                    Id = "DEP-S1",
                    Name = "Control de Calidad",
                    Type = DepartmentType.Service,
                    SecondaryCloseOrder = 1,
                    SecondaryAllocationBaseKey = "Horas-máquina",
                    Metrics = new()
                    {
                        ["Metros cuadrados"] = 40m,
                        ["Kilowatts"] = 25m
                    }
                },
                new Department
                {
                    Id = "DEP-S2",
                    Name = "Almacén",
                    Type = DepartmentType.Service,
                    SecondaryCloseOrder = 2,
                    SecondaryAllocationBaseKey = "Materia prima",
                    Metrics = new()
                    {
                        ["Metros cuadrados"] = 60m,
                        ["Kilowatts"] = 15m
                    }
                }
            ],
            OverheadExpenses =
            [
                new OverheadExpense { Id = "GIF-01", Concept = "Renta de planta", Amount = 32000m, AllocationType = AllocationType.MetricBased, AllocationBaseKey = "Metros cuadrados" },
                new OverheadExpense { Id = "GIF-02", Concept = "Energía eléctrica", Amount = 18000m, AllocationType = AllocationType.MetricBased, AllocationBaseKey = "Kilowatts" },
                new OverheadExpense { Id = "GIF-03", Concept = "Supervisor de mezclado", Amount = 14000m, AllocationType = AllocationType.DirectAssignment, TargetDepartmentId = "DEP-P1" },
                new OverheadExpense { Id = "GIF-04", Concept = "Supervisor de envasado", Amount = 13000m, AllocationType = AllocationType.DirectAssignment, TargetDepartmentId = "DEP-P2" },
                new OverheadExpense { Id = "GIF-05", Concept = "Personal de calidad", Amount = 11000m, AllocationType = AllocationType.DirectAssignment, TargetDepartmentId = "DEP-S1" },
                new OverheadExpense { Id = "GIF-06", Concept = "Personal de almacén", Amount = 9500m, AllocationType = AllocationType.DirectAssignment, TargetDepartmentId = "DEP-S2" }
            ],
            ProductionOrders =
            [
                new ProductionOrder
                {
                    Id = "ORD-01",
                    Code = "SkO:001v",
                    Status = OrderStatus.Finished,
                    InitialWorkInProcessCost = 12500m,
                    ProducedUnits = 5000m,
                    SoldUnits = 4200m,
                    DirectLaborEntries =
                    [
                        new DirectLaborEntry { DepartmentId = "DEP-P1", Hours = 140m },
                        new DirectLaborEntry { DepartmentId = "DEP-P2", Hours = 120m }
                    ]
                },
                new ProductionOrder
                {
                    Id = "ORD-02",
                    Code = "SkO:002v",
                    Status = OrderStatus.Finished,
                    InitialWorkInProcessCost = 0m,
                    ProducedUnits = 6000m,
                    SoldUnits = 5000m,
                    DirectLaborEntries =
                    [
                        new DirectLaborEntry { DepartmentId = "DEP-P1", Hours = 170m },
                        new DirectLaborEntry { DepartmentId = "DEP-P2", Hours = 150m }
                    ]
                },
                new ProductionOrder
                {
                    Id = "ORD-03",
                    Code = "SkO:003v",
                    Status = OrderStatus.InProcess,
                    InitialWorkInProcessCost = 0m,
                    ProducedUnits = 3000m,
                    SoldUnits = 0m,
                    DirectLaborEntries =
                    [
                        new DirectLaborEntry { DepartmentId = "DEP-P1", Hours = 85m },
                        new DirectLaborEntry { DepartmentId = "DEP-P2", Hours = 60m }
                    ]
                }
            ],
            Consumptions =
            [
                new Consumption { Id = "CON-01", MaterialId = "MAT-01", ProductionOrderId = "ORD-01", Date = new DateOnly(2026, 11, 6), Quantity = 320m },
                new Consumption { Id = "CON-02", MaterialId = "MAT-02", ProductionOrderId = "ORD-01", Date = new DateOnly(2026, 11, 6), Quantity = 180m },
                new Consumption { Id = "CON-03", MaterialId = "MAT-03", ProductionOrderId = "ORD-01", Date = new DateOnly(2026, 11, 6), Quantity = 1100m },
                new Consumption { Id = "CON-04", MaterialId = "MAT-04", ProductionOrderId = "ORD-01", Date = new DateOnly(2026, 11, 6), Quantity = 12m },
                new Consumption { Id = "CON-05", MaterialId = "MAT-05", ProductionOrderId = "ORD-01", Date = new DateOnly(2026, 11, 6), Quantity = 5000m },
                new Consumption { Id = "CON-06", MaterialId = "MAT-06", ProductionOrderId = "ORD-01", Date = new DateOnly(2026, 11, 6), Quantity = 5000m },
                new Consumption { Id = "CON-07", MaterialId = "MAT-07", ProductionOrderId = "ORD-01", Date = new DateOnly(2026, 11, 6), Quantity = 210m },

                new Consumption { Id = "CON-08", MaterialId = "MAT-01", ProductionOrderId = "ORD-02", Date = new DateOnly(2026, 11, 10), Quantity = 380m },
                new Consumption { Id = "CON-09", MaterialId = "MAT-02", ProductionOrderId = "ORD-02", Date = new DateOnly(2026, 11, 10), Quantity = 220m },
                new Consumption { Id = "CON-10", MaterialId = "MAT-03", ProductionOrderId = "ORD-02", Date = new DateOnly(2026, 11, 10), Quantity = 1350m },
                new Consumption { Id = "CON-11", MaterialId = "MAT-04", ProductionOrderId = "ORD-02", Date = new DateOnly(2026, 11, 10), Quantity = 15m },
                new Consumption { Id = "CON-12", MaterialId = "MAT-05", ProductionOrderId = "ORD-02", Date = new DateOnly(2026, 11, 10), Quantity = 6000m },
                new Consumption { Id = "CON-13", MaterialId = "MAT-06", ProductionOrderId = "ORD-02", Date = new DateOnly(2026, 11, 10), Quantity = 6000m },
                new Consumption { Id = "CON-14", MaterialId = "MAT-07", ProductionOrderId = "ORD-02", Date = new DateOnly(2026, 11, 10), Quantity = 250m },

                new Consumption { Id = "CON-15", MaterialId = "MAT-01", ProductionOrderId = "ORD-03", Date = new DateOnly(2026, 11, 15), Quantity = 190m },
                new Consumption { Id = "CON-16", MaterialId = "MAT-02", ProductionOrderId = "ORD-03", Date = new DateOnly(2026, 11, 15), Quantity = 110m },
                new Consumption { Id = "CON-17", MaterialId = "MAT-03", ProductionOrderId = "ORD-03", Date = new DateOnly(2026, 11, 15), Quantity = 650m },
                new Consumption { Id = "CON-18", MaterialId = "MAT-04", ProductionOrderId = "ORD-03", Date = new DateOnly(2026, 11, 15), Quantity = 8m },
                new Consumption { Id = "CON-19", MaterialId = "MAT-05", ProductionOrderId = "ORD-03", Date = new DateOnly(2026, 11, 15), Quantity = 3000m },
                new Consumption { Id = "CON-20", MaterialId = "MAT-06", ProductionOrderId = "ORD-03", Date = new DateOnly(2026, 11, 15), Quantity = 3000m }
            ]
        };

        return Task.FromResult(project);
    }

    public Task SaveAsync(CostProject project, string filePath)
    {
        var json = JsonSerializer.Serialize(project, s_serializerOptions);
        return File.WriteAllTextAsync(filePath, json);
    }

    public async Task<CostProject?> LoadAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<CostProject>(json, s_serializerOptions);
    }
}