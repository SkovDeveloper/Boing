namespace Boing.Dialogs;

public sealed partial class MaterialDialog
{
    public MaterialDialogViewModel ViewModel { get; }

    public MaterialDialog(Material? existingMaterial = null)
    {
        ViewModel = new MaterialDialogViewModel(existingMaterial)
        {
            CloseDialogAction = Hide
        };
        InitializeComponent();
    }
}