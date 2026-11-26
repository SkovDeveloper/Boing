namespace Boing.Interfaces;

public interface IProjectStateService
{
    CostProject CurrentProject { get; }
    CostCalculationResult CurrentResult { get; }
    event EventHandler? StateChanged;
    Task InitializeAsync();
    void LoadProject(CostProject project);
    void Recalculate();
}