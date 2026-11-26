namespace Boing.Dialogs;

public sealed partial class MovementDialog
{
    public MovementDialogViewModel ViewModel { get; }

    public MovementDialog(
        Material targetMaterial,
        IReadOnlyList<ProductionOrder> availableOrders,
        Purchase? existingPurchase = null,
        Consumption? existingConsumption = null)
    {
        ViewModel = new MovementDialogViewModel(targetMaterial, availableOrders, existingPurchase, existingConsumption)
        {
            CloseDialogAction = Hide
        };
        InitializeComponent();
    }
}