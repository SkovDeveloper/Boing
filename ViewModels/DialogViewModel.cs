namespace Boing.ViewModels;

public abstract partial class DialogViewModel : ObservableObject
{
    public Action? CloseDialogAction { get; set; }

    public bool IsOperationSuccessful { get; protected set; }

    public bool IsProcessing => NotificationContext.IsOpen &&
                                NotificationContext.Severity == InfoBarSeverity.Success;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsProcessing))]
    public partial NotificationStatusContext NotificationContext { get; set; } = new();

    [RelayCommand]
    private Task SubmitFormAsync() => ExecuteSubmitActionAsync();

    protected abstract Task ExecuteSubmitActionAsync();

    protected void PublishNotification()
    {
        NotificationContext.IsOpen = true;
        OnPropertyChanged(nameof(IsProcessing));
    }

    protected void ShowError(string title, string message)
    {
        NotificationContext.Severity = InfoBarSeverity.Error;
        NotificationContext.Title = title;
        NotificationContext.Message = message;
        PublishNotification();
    }

    public void HideNotification()
    {
        NotificationContext.IsOpen = false;
        OnPropertyChanged(nameof(IsProcessing));
    }

    public void DismissNotification()
    {
        HideNotification();
        CancelForm();
    }

    [RelayCommand]
    private void CancelForm() => CloseDialogAction?.Invoke();
}