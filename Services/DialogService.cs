namespace Boing.Services;

public sealed class DialogService : IDialogService
{
    private Func<XamlRoot?>? _xamlRootProvider;

    public void Initialize(Func<XamlRoot?> xamlRootProvider) =>
        _xamlRootProvider = xamlRootProvider;

    public async Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog)
    {
        var root = _xamlRootProvider?.Invoke();
        if (root is null)
        {
            return ContentDialogResult.None;
        }

        dialog.XamlRoot = root;
        return await dialog.ShowAsync();
    }

    public Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "Aceptar",
            DefaultButton = ContentDialogButton.Close
        };

        return ShowDialogAsync(dialog);
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "Confirmar",
        string cancelText = "Cancelar")
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = confirmText,
            CloseButtonText = cancelText,
            DefaultButton = ContentDialogButton.Close
        };

        var result = await ShowDialogAsync(dialog);
        return result == ContentDialogResult.Primary;
    }
}