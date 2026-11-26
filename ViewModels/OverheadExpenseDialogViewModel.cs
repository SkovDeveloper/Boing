namespace Boing.ViewModels;

public sealed partial class OverheadExpenseDialogViewModel : DialogViewModel
{
    private readonly string _expenseId;

    public IReadOnlyList<string> AllocationTypeOptions { get; } = ["Por base métrica", "Asignación directa"];
    public IReadOnlyList<string> MetricBaseOptions { get; } = ["Metros cuadrados", "Kilowatts", "Horas-máquina", "Materia prima"];
    public IReadOnlyList<Department> Departments { get; }

    public string DialogTitle { get; }
    public string DialogCaption { get; }
    public string SubmitButtonText { get; }

    public OverheadExpense? ResultExpense { get; private set; }

    [ObservableProperty]
    public partial string Concept { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double Amount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MetricVisibility))]
    [NotifyPropertyChangedFor(nameof(DirectVisibility))]
    public partial int SelectedAllocationTypeIndex { get; set; }

    [ObservableProperty]
    public partial string AllocationBaseKey { get; set; } = "Metros cuadrados";

    [ObservableProperty]
    public partial Department? SelectedTargetDepartment { get; set; }

    public Visibility MetricVisibility => SelectedAllocationTypeIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility DirectVisibility => SelectedAllocationTypeIndex == 1 ? Visibility.Visible : Visibility.Collapsed;

    public OverheadExpenseDialogViewModel(
        IReadOnlyList<Department> availableDepartments,
        OverheadExpense? existingExpense = null)
    {
        Departments = availableDepartments;
        SelectedTargetDepartment = availableDepartments.FirstOrDefault();

        if (existingExpense is null)
        {
            _expenseId = $"GIF-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            DialogTitle = "Nuevo gasto indirecto";
            DialogCaption = "Registra un gasto indirecto de fabricación (renta, energía, nómina indirecta, etc.) para el prorrateo primario.";
            SubmitButtonText = "Agregar";
        }
        else
        {
            _expenseId = existingExpense.Id;
            DialogTitle = "Editar gasto indirecto";
            DialogCaption = "Modifica el concepto, monto o criterio de distribución de este gasto GIF.";
            SubmitButtonText = "Guardar cambios";

            Concept = existingExpense.Concept;
            Amount = (double)existingExpense.Amount;
            SelectedAllocationTypeIndex = existingExpense.AllocationType == AllocationType.MetricBased ? 0 : 1;
            AllocationBaseKey = string.IsNullOrWhiteSpace(existingExpense.AllocationBaseKey)
                ? "Metros cuadrados"
                : existingExpense.AllocationBaseKey;
            SelectedTargetDepartment = availableDepartments.FirstOrDefault(d => d.Id == existingExpense.TargetDepartmentId)
                                       ?? availableDepartments.FirstOrDefault();
        }
    }

    protected override Task ExecuteSubmitActionAsync()
    {
        if (string.IsNullOrWhiteSpace(Concept))
        {
            ShowError("Concepto requerido", "Ingresa el nombre o concepto del gasto indirecto.");
            return Task.CompletedTask;
        }

        if (double.IsNaN(Amount) || Amount <= 0)
        {
            ShowError("Monto inválido", "El monto del gasto debe ser mayor a cero.");
            return Task.CompletedTask;
        }

        var isMetric = SelectedAllocationTypeIndex == 0;

        if (isMetric && string.IsNullOrWhiteSpace(AllocationBaseKey))
        {
            ShowError("Base requerida", "Selecciona o escribe una base métrica de distribución.");
            return Task.CompletedTask;
        }

        if (!isMetric && SelectedTargetDepartment is null)
        {
            ShowError("Departamento requerido", "Selecciona el departamento al que se asigna directamente este gasto.");
            return Task.CompletedTask;
        }

        ResultExpense = new OverheadExpense
        {
            Id = _expenseId,
            Concept = Concept.Trim(),
            Amount = (decimal)Amount,
            AllocationType = isMetric ? AllocationType.MetricBased : AllocationType.DirectAssignment,
            AllocationBaseKey = isMetric ? AllocationBaseKey.Trim() : null,
            TargetDepartmentId = isMetric ? null : SelectedTargetDepartment?.Id
        };

        IsOperationSuccessful = true;
        CloseDialogAction?.Invoke();
        return Task.CompletedTask;
    }
}