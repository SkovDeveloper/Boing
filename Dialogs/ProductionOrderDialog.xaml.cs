namespace Boing.Dialogs;

public sealed partial class ProductionOrderDialog
{
    public ProductionOrderDialogViewModel ViewModel { get; }

    public ProductionOrderDialog(
        IReadOnlyList<Department> productiveDepartments,
        string defaultCode,
        ProductionOrder? existingOrder = null)
    {
        ViewModel = new ProductionOrderDialogViewModel(productiveDepartments, defaultCode, existingOrder)
        {
            CloseDialogAction = Hide
        };
        InitializeComponent();
    }
}