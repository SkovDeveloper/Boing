namespace Boing.Dialogs;

public sealed partial class StatementParametersDialog
{
    public StatementParametersDialogViewModel ViewModel { get; }

    public StatementParametersDialog(decimal currentInitialFg, decimal currentSelling, decimal currentAdmin)
    {
        ViewModel = new StatementParametersDialogViewModel(currentInitialFg, currentSelling, currentAdmin)
        {
            CloseDialogAction = Hide
        };
        InitializeComponent();
    }
}