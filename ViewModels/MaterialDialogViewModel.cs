namespace Boing.ViewModels;

public sealed partial class MaterialDialogViewModel : DialogViewModel
{
    private readonly string _materialId;

    public IReadOnlyList<string> UnitOptions { get; } = ["kg", "L", "pzas", "g", "ml"];
    public IReadOnlyList<string> ClassificationOptions { get; } = ["Directo", "Indirecto"];

    public string DialogTitle { get; }
    public string DialogCaption { get; }
    public string SubmitButtonText { get; }

    public Material? ResultMaterial { get; private set; }

    [ObservableProperty] public partial string Name { get; set; } = string.Empty;

    [ObservableProperty] public partial string Unit { get; set; } = "kg";

    [ObservableProperty] public partial int SelectedClassificationIndex { get; set; }

    [ObservableProperty] public partial double InitialQuantity { get; set; }

    [ObservableProperty] public partial double InitialUnitCost { get; set; }

    public MaterialDialogViewModel(Material? existingMaterial = null)
    {
        if (existingMaterial is null)
        {
            _materialId = $"MAT-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            DialogTitle = "Nuevo ingrediente";
            DialogCaption = "Crea una nueva tarjeta de almacén definiendo su unidad, clasificación y saldo inicial.";
            SubmitButtonText = "Agregar";
        }
        else
        {
            _materialId = existingMaterial.Id;
            DialogTitle = "Editar ingrediente";
            DialogCaption = "Actualiza los datos generales o el saldo inicial de esta tarjeta de almacén.";
            SubmitButtonText = "Guardar cambios";

            Name = existingMaterial.Name;
            Unit = existingMaterial.Unit;
            SelectedClassificationIndex = existingMaterial.Classification == MaterialClassification.Direct ? 0 : 1;
            InitialQuantity = (double)existingMaterial.InitialQuantity;
            InitialUnitCost = (double)existingMaterial.InitialUnitCost;
        }
    }

    protected override Task ExecuteSubmitActionAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ShowError("Nombre requerido", "Ingresa el nombre del ingrediente.");
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(Unit))
        {
            ShowError("Unidad requerida", "Selecciona o escribe una unidad de medida.");
            return Task.CompletedTask;
        }

        if (double.IsNaN(InitialQuantity) || InitialQuantity < 0)
        {
            ShowError("Cantidad inválida", "La cantidad inicial debe ser mayor o igual a cero.");
            return Task.CompletedTask;
        }

        if (double.IsNaN(InitialUnitCost) || InitialUnitCost < 0)
        {
            ShowError("Costo inválido", "El costo unitario inicial debe ser mayor o igual a cero.");
            return Task.CompletedTask;
        }

        ResultMaterial = new Material
        {
            Id = _materialId,
            Name = Name.Trim(),
            Unit = Unit.Trim(),
            Classification = SelectedClassificationIndex == 0
                ? MaterialClassification.Direct
                : MaterialClassification.Indirect,
            InitialQuantity = (decimal)InitialQuantity,
            InitialUnitCost = (decimal)InitialUnitCost
        };

        IsOperationSuccessful = true;
        CloseDialogAction?.Invoke();
        return Task.CompletedTask;
    }
}