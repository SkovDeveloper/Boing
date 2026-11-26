using System.Collections.ObjectModel;

namespace Boing.ViewModels;

public sealed partial class LaborEntryInputItem : ObservableObject
{
    public string DepartmentId { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;

    [ObservableProperty] public partial double Hours { get; set; }
}

public sealed partial class ProductionOrderDialogViewModel : DialogViewModel
{
    private readonly string _orderId;

    public IReadOnlyList<string> StatusOptions { get; } = ["Terminada", "En proceso (IFPP)"];
    public ObservableCollection<LaborEntryInputItem> LaborEntries { get; } = [];

    public string DialogTitle { get; }
    public string DialogCaption { get; }
    public string SubmitButtonText { get; }

    public ProductionOrder? ResultOrder { get; private set; }

    [ObservableProperty] public partial string Code { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SoldUnitsVisibility))]
    public partial int SelectedStatusIndex { get; set; }

    [ObservableProperty] public partial double InitialWorkInProcessCost { get; set; }

    [ObservableProperty] public partial double ProducedUnits { get; set; }

    [ObservableProperty] public partial double SoldUnits { get; set; }

    public Visibility SoldUnitsVisibility => SelectedStatusIndex == 0 ? Visibility.Visible : Visibility.Collapsed;

    public ProductionOrderDialogViewModel(
        IReadOnlyList<Department> productiveDepartments,
        string defaultCode,
        ProductionOrder? existingOrder = null)
    {
        if (existingOrder is null)
        {
            _orderId = $"ORD-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            DialogTitle = "Nueva orden de producción";
            DialogCaption =
                "Define el estado, unidades producidas/vendidas y horas de mano de obra directa por departamento.";
            SubmitButtonText = "Crear orden";
            Code = defaultCode;
        }
        else
        {
            _orderId = existingOrder.Id;
            DialogTitle = "Editar orden de producción";
            DialogCaption = "Actualiza el estatus (Terminada o IFPP), unidades y horas MOD trabajadas en esta orden.";
            SubmitButtonText = "Guardar cambios";

            Code = existingOrder.Code;
            SelectedStatusIndex = existingOrder.Status == OrderStatus.Finished ? 0 : 1;
            InitialWorkInProcessCost = (double)existingOrder.InitialWorkInProcessCost;
            ProducedUnits = (double)existingOrder.ProducedUnits;
            SoldUnits = (double)existingOrder.SoldUnits;
        }

        foreach (var dept in productiveDepartments)
        {
            var existingHours = existingOrder?.DirectLaborEntries
                .FirstOrDefault(e => e.DepartmentId == dept.Id)?.Hours ?? 0m;

            LaborEntries.Add(new LaborEntryInputItem
            {
                DepartmentId = dept.Id,
                DepartmentName = $"{dept.Name} ({dept.DirectLaborHourlyRate:C2}/hr)",
                Hours = (double)existingHours
            });
        }
    }

    protected override Task ExecuteSubmitActionAsync()
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            ShowError("Folio requerido", "Ingresa el código o folio de la orden de producción.");
            return Task.CompletedTask;
        }

        if (double.IsNaN(ProducedUnits) || ProducedUnits <= 0)
        {
            ShowError("Unidades inválidas", "Las unidades producidas deben ser mayores a cero.");
            return Task.CompletedTask;
        }

        if (double.IsNaN(InitialWorkInProcessCost) || InitialWorkInProcessCost < 0)
        {
            ShowError("Saldo inicial inválido", "El costo inicial en proceso debe ser mayor o igual a cero.");
            return Task.CompletedTask;
        }

        var isFinished = SelectedStatusIndex == 0;
        var finalSoldUnits = isFinished ? (double.IsNaN(SoldUnits) ? 0 : SoldUnits) : 0;

        if (isFinished && (finalSoldUnits < 0 || finalSoldUnits > ProducedUnits))
        {
            ShowError("Unidades vendidas inválidas",
                "Las unidades vendidas deben estar entre 0 y las unidades producidas.");
            return Task.CompletedTask;
        }

        var entries = LaborEntries
            .Where(e => !double.IsNaN(e.Hours) && e.Hours > 0)
            .Select(e => new DirectLaborEntry
            {
                DepartmentId = e.DepartmentId,
                Hours = (decimal)e.Hours
            })
            .ToList();

        ResultOrder = new ProductionOrder
        {
            Id = _orderId,
            Code = Code.Trim(),
            Status = isFinished ? OrderStatus.Finished : OrderStatus.InProcess,
            InitialWorkInProcessCost = (decimal)InitialWorkInProcessCost,
            ProducedUnits = (decimal)ProducedUnits,
            SoldUnits = (decimal)finalSoldUnits,
            DirectLaborEntries = entries
        };

        IsOperationSuccessful = true;
        CloseDialogAction?.Invoke();
        return Task.CompletedTask;
    }
}