namespace Boing.Interfaces;

public interface IProjectStorageService
{
    string AutoSavePath { get; }
    Task SaveAsync<T>(string filePath, T data, CancellationToken cancellationToken = default);
    Task<T?> LoadAsync<T>(string filePath, CancellationToken cancellationToken = default);
    Task SaveAutoSaveAsync<T>(T data, CancellationToken cancellationToken = default);
    Task<T?> LoadAutoSaveAsync<T>(CancellationToken cancellationToken = default);

    Task<CostProject> LoadDefaultScenarioAsync();
    void ClearAutoSave();
}