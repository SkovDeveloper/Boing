namespace Boing.ViewModels;

public sealed partial class MovementDialogViewModel : DialogViewModel
{
    private readonly string _materialId;
    private readonly string _movementId;

    public IReadOnlyList<string> MovementTypeOptions { get; } = ["Compra (Entrada)", "Consumo (Salida)"];
    public IReadOnlyList<ProductionOrder> Orders { get; }

    public string DialogTitle { get; }
    public string DialogCaption { get; }
    public string SubmitButtonText { get; }
    public bool IsTypeEditable { get; }

    public Purchase? ResultPurchase { get; private set; }
    public Consumption? ResultConsumption { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PurchaseVisibility))]
    [NotifyPropertyChangedFor(nameof(ConsumptionVisibility))]
    public partial int SelectedMovementTypeIndex { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? Date { get; set; } = DateTimeOffset.Now;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstimatedTotalText))]
    public partial double Quantity { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstimatedTotalText))]
    public partial double UnitCost { get; set; }

    [ObservableProperty]
    public partial ProductionOrder? SelectedOrder { get; set; }

    public Visibility PurchaseVisibility => SelectedMovementTypeIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ConsumptionVisibility => SelectedMovementTypeIndex == 1 ? Visibility.Visible : Visibility.Collapsed;

    public string EstimatedTotalText
    {
        get
        {
            if (double.IsNaN(Quantity) || double.IsNaN(UnitCost) || Quantity <= 0 || UnitCost <= 0)
            {
                return "Importe estimado: $0.00";
            }

            var total = (decimal)Quantity * (decimal)UnitCost;
            return $"Importe estimado: {total:C2}";
        }
    }

    public MovementDialogViewModel(
        Material targetMaterial,
        IReadOnlyList<ProductionOrder> availableOrders,
        Purchase? existingPurchase = null,
        Consumption? existingConsumption = null)
    {
        _materialId = targetMaterial.Id;
        Orders = availableOrders;
        SelectedOrder = availableOrders.FirstOrDefault();

        if (existingPurchase is not null)
        {
            _movementId = existingPurchase.Id;
            DialogTitle = "Editar compra";
            DialogCaption = $"{targetMaterial.Name} ({targetMaterial.Unit}) · Al guardar se recalculará toda la tarjeta.";
            SubmitButtonText = "Guardar cambios";
            IsTypeEditable = false;
            SelectedMovementTypeIndex = 0;
            Date = new DateTimeOffset(existingPurchase.Date.ToDateTime(TimeOnly.MinValue));
            Quantity = (double)existingPurchase.Quantity;
            UnitCost = (double)existingPurchase.UnitCost;
        }
        else if (existingConsumption is not null)
        {
            _movementId = existingConsumption.Id;
            DialogTitle = "Editar consumo";
            DialogCaption = $"{targetMaterial.Name} ({targetMaterial.Unit}) · Si estaba dividido por capas, se recalculará con la cantidad total.";
            SubmitButtonText = "Guardar cambios";
            IsTypeEditable = false;
            SelectedMovementTypeIndex = 1;
            Date = new DateTimeOffset(existingConsumption.Date.ToDateTime(TimeOnly.MinValue));
            Quantity = (double)existingConsumption.Quantity;
            SelectedOrder = availableOrders.FirstOrDefault(o => o.Id == existingConsumption.ProductionOrderId)
                            ?? availableOrders.FirstOrDefault();
        }
        else
        {
            _movementId = string.Empty;
            DialogTitle = "Registrar movimiento";
            DialogCaption = $"{targetMaterial.Name} ({targetMaterial.Unit}) · Agrega una compra o un consumo a la tarjeta de almacén.";
            SubmitButtonText = "Registrar";
            IsTypeEditable = true;
            SelectedMovementTypeIndex = 0;
        }
    }

    protected override Task ExecuteSubmitActionAsync()
    {
        if (Date is null)
        {
            ShowError("Fecha requerida", "Selecciona una fecha para el movimiento.");
            return Task.CompletedTask;
        }

        if (double.IsNaN(Quantity) || Quantity <= 0)
        {
            ShowError("Cantidad inválida", "La cantidad debe ser mayor a cero.");
            return Task.CompletedTask;
        }

        var movementDate = DateOnly.FromDateTime(Date.Value.DateTime);

        if (SelectedMovementTypeIndex == 0)
        {
            if (double.IsNaN(UnitCost) || UnitCost <= 0)
            {
                ShowError("Costo unitario inválido", "Ingresa un costo unitario mayor a cero para la compra.");
                return Task.CompletedTask;
            }

            var purchaseId = string.IsNullOrWhiteSpace(_movementId)
                ? $"PUR-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}"
                : _movementId;

            ResultPurchase = new Purchase
            {
                Id = purchaseId,
                MaterialId = _materialId,
                Date = movementDate,
                Quantity = (decimal)Quantity,
                UnitCost = (decimal)UnitCost
            };
        }
        else
        {
            if (SelectedOrder is null)
            {
                ShowError("Orden requerida", "Selecciona la orden de producción a la que se asigna el consumo.");
                return Task.CompletedTask;
            }

            var consumptionId = string.IsNullOrWhiteSpace(_movementId)
                ? $"CON-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}"
                : _movementId;

            ResultConsumption = new Consumption
            {
                Id = consumptionId,
                MaterialId = _materialId,
                ProductionOrderId = SelectedOrder.Id,
                Date = movementDate,
                Quantity = (decimal)Quantity
            };
        }

        IsOperationSuccessful = true;
        CloseDialogAction?.Invoke();
        return Task.CompletedTask;
    }
}