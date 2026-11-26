namespace Boing.Dialogs;

public sealed partial class DepartmentDialog
{
    public DepartmentDialogViewModel ViewModel { get; }

    public DepartmentDialog(string assignedAlias, Department? existingDepartment = null)
    {
        ViewModel = new DepartmentDialogViewModel(assignedAlias, existingDepartment);
        ViewModel.CloseDialogAction = Hide;
        InitializeComponent();
    }
}