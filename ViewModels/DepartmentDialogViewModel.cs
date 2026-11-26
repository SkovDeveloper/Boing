namespace Boing.ViewModels;

public sealed partial class DepartmentDialogViewModel : DialogViewModel
{
    private readonly string _departmentId;
    private readonly Dictionary<string, decimal> _preservedMetrics;

    public IReadOnlyList<string> TypeOptions { get; } = ["Productivo", "De servicio"];

    public IReadOnlyList<string> SecondaryBaseOptions { get; } =
        ["Horas-máquina", "Materia prima", "Metros cuadrados", "Kilowatts"];

    public string Alias { get; }
    public string DialogTitle { get; }
    public string DialogCaption { get; }
    public string SubmitButtonText { get; }

    public Department? ResultDepartment { get; private set; }

    [ObservableProperty] public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProductiveVisibility))]
    [NotifyPropertyChangedFor(nameof(ServiceVisibility))]
    public partial int SelectedTypeIndex { get; set; }

    [ObservableProperty] public partial double DirectLaborHourlyRate { get; set; }

    [ObservableProperty] public partial double SecondaryCloseOrder { get; set; } = 1;

    [ObservableProperty] public partial string SecondaryAllocationBaseKey { get; set; } = "Horas-máquina";

    [ObservableProperty] public partial double SquareMeters { get; set; }

    [ObservableProperty] public partial double Kilowatts { get; set; }

    [ObservableProperty] public partial double MachineHours { get; set; }

    [ObservableProperty] public partial double RawMaterialBase { get; set; }

    public Visibility ProductiveVisibility => SelectedTypeIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ServiceVisibility => SelectedTypeIndex == 1 ? Visibility.Visible : Visibility.Collapsed;

    public DepartmentDialogViewModel(string assignedAlias, Department? existingDepartment = null)
    {
        Alias = assignedAlias;

        if (existingDepartment is null)
        {
            _departmentId = $"DEP-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            _preservedMetrics = [];
            DialogTitle = "Nuevo departamento";
            DialogCaption =
                "Define un departamento productivo o de servicio, su tarifa de mano de obra directa y sus bases métricas.";
            SubmitButtonText = "Agregar";
        }
        else
        {
            _departmentId = existingDepartment.Id;
            _preservedMetrics = new Dictionary<string, decimal>(existingDepartment.Metrics);
            DialogTitle = "Editar departamento";
            DialogCaption = "Actualiza el tipo, tarifa de MOD o métricas de distribución de este departamento.";
            SubmitButtonText = "Guardar cambios";

            Name = existingDepartment.Name;
            SelectedTypeIndex = existingDepartment.Type == DepartmentType.Productive ? 0 : 1;
            DirectLaborHourlyRate = (double)(existingDepartment.DirectLaborHourlyRate ?? 0m);
            SecondaryCloseOrder = existingDepartment.SecondaryCloseOrder is > 0
                ? existingDepartment.SecondaryCloseOrder.Value
                : 1;
            SecondaryAllocationBaseKey = string.IsNullOrWhiteSpace(existingDepartment.SecondaryAllocationBaseKey)
                ? "Horas-máquina"
                : existingDepartment.SecondaryAllocationBaseKey;

            SquareMeters = (double)existingDepartment.Metrics.GetValueOrDefault("Metros cuadrados", 0m);
            Kilowatts = (double)existingDepartment.Metrics.GetValueOrDefault("Kilowatts", 0m);
            MachineHours = (double)existingDepartment.Metrics.GetValueOrDefault("Horas-máquina", 0m);
            RawMaterialBase = (double)existingDepartment.Metrics.GetValueOrDefault("Materia prima", 0m);
        }
    }

    protected override Task ExecuteSubmitActionAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ShowError("Nombre requerido", "Ingresa el nombre del departamento.");
            return Task.CompletedTask;
        }

        var isProductive = SelectedTypeIndex == 0;

        if (isProductive && (double.IsNaN(DirectLaborHourlyRate) || DirectLaborHourlyRate < 0))
        {
            ShowError("Tarifa inválida", "La tarifa de mano de obra directa debe ser mayor o igual a cero.");
            return Task.CompletedTask;
        }

        if (!isProductive && (double.IsNaN(SecondaryCloseOrder) || SecondaryCloseOrder < 1))
        {
            ShowError("Orden de cierre inválido", "El orden de cierre debe ser un número mayor o igual a 1.");
            return Task.CompletedTask;
        }

        if (!isProductive && string.IsNullOrWhiteSpace(SecondaryAllocationBaseKey))
        {
            ShowError("Base de cierre requerida",
                "Indica la base con la que se cerrará este departamento de servicio.");
            return Task.CompletedTask;
        }

        var updatedMetrics = new Dictionary<string, decimal>(_preservedMetrics)
        {
            ["Metros cuadrados"] = (decimal)Math.Max(0, double.IsNaN(SquareMeters) ? 0 : SquareMeters),
            ["Kilowatts"] = (decimal)Math.Max(0, double.IsNaN(Kilowatts) ? 0 : Kilowatts)
        };

        if (isProductive)
        {
            updatedMetrics["Horas-máquina"] = (decimal)Math.Max(0, double.IsNaN(MachineHours) ? 0 : MachineHours);
            updatedMetrics["Materia prima"] = (decimal)Math.Max(0, double.IsNaN(RawMaterialBase) ? 0 : RawMaterialBase);
            updatedMetrics.TryAdd("IndirectMaterialBase", 50m);
        }

        ResultDepartment = new Department
        {
            Id = _departmentId,
            Name = Name.Trim(),
            Type = isProductive ? DepartmentType.Productive : DepartmentType.Service,
            DirectLaborHourlyRate = isProductive ? (decimal)DirectLaborHourlyRate : 0m,
            SecondaryCloseOrder = isProductive ? 0 : (int)Math.Round(SecondaryCloseOrder),
            SecondaryAllocationBaseKey = isProductive ? null : SecondaryAllocationBaseKey.Trim(),
            Metrics = updatedMetrics
        };

        IsOperationSuccessful = true;
        CloseDialogAction?.Invoke();
        return Task.CompletedTask;
    }
}