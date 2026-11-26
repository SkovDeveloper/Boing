using Boing.Interfaces;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Boing.Services;

public sealed class FilePickerService : IFilePickerService
{
    public Task<string?> PickOpenJsonPathAsync() =>
        PickOpenFileAsync([".boing", ".json"]);

    public Task<string?> PickSaveJsonPathAsync(string suggestedFileName = "Boing de Mango") =>
        PickSaveFileAsync(suggestedFileName, "Proyecto Boing", ".boing");

    public async Task<string?> PickOpenFileAsync(IReadOnlyList<string> fileTypes)
    {
        if (MainWindow.Current is null)
        {
            return null;
        }

        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.List,
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };

        foreach (var fileType in fileTypes)
        {
            picker.FileTypeFilter.Add(fileType);
        }

        var hwnd = WindowNative.GetWindowHandle(MainWindow.Current);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    public async Task<string?> PickSaveFileAsync(string suggestedFileName, string fileTypeLabel, string extension)
    {
        if (MainWindow.Current is null)
        {
            return null;
        }

        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = suggestedFileName
        };

        picker.FileTypeChoices.Add(fileTypeLabel, [extension]);

        var hwnd = WindowNative.GetWindowHandle(MainWindow.Current);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        return file?.Path;
    }
}