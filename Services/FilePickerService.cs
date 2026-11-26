using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Boing.Services;

public sealed class FilePickerService : IFilePickerService
{
    private nint _windowHandle;

    public void Initialize(nint windowHandle) =>
        _windowHandle = windowHandle;

    public async Task<string?> PickOpenJsonPathAsync()
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".json");
        InitializeWithWindow.Initialize(picker, _windowHandle);

        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    public async Task<string?> PickSaveJsonPathAsync(string suggestedFileName)
    {
        var picker = new FileSavePicker
        {
            SuggestedFileName = suggestedFileName
        };
        picker.FileTypeChoices.Add("JSON File", [".json"]);
        InitializeWithWindow.Initialize(picker, _windowHandle);

        var file = await picker.PickSaveFileAsync();
        return file?.Path;
    }
}