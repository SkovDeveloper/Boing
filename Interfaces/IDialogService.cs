namespace Boing.Interfaces;

public interface IDialogService
{
    void Initialize(Func<XamlRoot?> xamlRootProvider);
    Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog);
    Task ShowMessageAsync(string title, string message);

    Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "Confirm",
        string cancelText = "Cancel");
}