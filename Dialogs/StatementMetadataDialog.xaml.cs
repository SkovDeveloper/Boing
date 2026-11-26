namespace Boing.Dialogs;

public sealed partial class StatementMetadataDialog
{
    public StatementMetadataDialogViewModel ViewModel { get; }

    public StatementMetadataDialog(
        string currentCompanyName,
        DateOnly currentStart,
        DateOnly currentEnd,
        string currentPreparedBy,
        string currentAuthorizedBy)
    {
        ViewModel = new StatementMetadataDialogViewModel(
            currentCompanyName,
            currentStart,
            currentEnd,
            currentPreparedBy,
            currentAuthorizedBy)
        {
            CloseDialogAction = Hide
        };
        InitializeComponent();
    }
}