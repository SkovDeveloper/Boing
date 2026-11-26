namespace Boing.Dialogs;

public sealed partial class InitialFinishedGoodsDialog
{
    public InitialFinishedGoodsDialogViewModel ViewModel { get; }

    public InitialFinishedGoodsDialog(decimal currentAmount)
    {
        ViewModel = new InitialFinishedGoodsDialogViewModel(currentAmount)
        {
            CloseDialogAction = Hide
        };
        InitializeComponent();
    }
}