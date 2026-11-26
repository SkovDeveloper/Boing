namespace Boing.Interfaces;

public interface IProjectStorageService
{
    Task<CostProject> LoadDefaultScenarioAsync();
    Task SaveAsync(CostProject project, string filePath);
    Task<CostProject?> LoadAsync(string filePath);
}