namespace Boing.Dialogs;

public sealed partial class OverheadExpenseDialog
{
    public OverheadExpenseDialogViewModel ViewModel { get; }

    public OverheadExpenseDialog(IReadOnlyList<Department> availableDepartments, OverheadExpense? existingExpense = null)
    {
        ViewModel = new OverheadExpenseDialogViewModel(availableDepartments, existingExpense)
        {
            CloseDialogAction = Hide
        };
        InitializeComponent();
    }
}