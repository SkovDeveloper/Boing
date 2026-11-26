namespace Boing.Interfaces;

public interface IFilePickerService
{
    Task<string?> PickOpenFileAsync(IReadOnlyList<string> fileTypes);
    Task<string?> PickSaveFileAsync(string suggestedFileName, string fileTypeLabel, string extension);
    Task<string?> PickOpenJsonPathAsync();
    Task<string?> PickSaveJsonPathAsync(string suggestedFileName = "Boing de Mango");
}