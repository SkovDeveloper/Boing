namespace Boing.Interfaces;

public interface IFilePickerService
{
    void Initialize(nint windowHandle);
    Task<string?> PickOpenJsonPathAsync();
    Task<string?> PickSaveJsonPathAsync(string suggestedFileName);
}