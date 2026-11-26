namespace Boing.Services;

public sealed class ProjectStateService : IProjectStateService
{
    private readonly ICostEngine _costEngine;
    private readonly IProjectStorageService _storageService;

    public CostProject CurrentProject { get; private set; } = new();
    public CostCalculationResult CurrentResult { get; private set; } = new();
    public event EventHandler? StateChanged;

    public ProjectStateService(ICostEngine costEngine, IProjectStorageService storageService)
    {
        _costEngine = costEngine;
        _storageService = storageService;
    }

    public async Task InitializeAsync()
    {
        CurrentProject = await _storageService.LoadDefaultScenarioAsync();
        Recalculate();
    }

    public void LoadProject(CostProject project)
    {
        CurrentProject = project;
        Recalculate();
    }

    public void Recalculate()
    {
        CurrentResult = _costEngine.CalculateAll(CurrentProject);
        StateChanged?.Invoke(this, EventArgs.Empty);

        ScheduleAutoSave();
    }

    private CancellationTokenSource? _autoSaveCts;

    private void ScheduleAutoSave()
    {
        _autoSaveCts?.Cancel();
        _autoSaveCts?.Dispose();
        _autoSaveCts = new CancellationTokenSource();
        var token = _autoSaveCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, token);
                await _storageService.SaveAutoSaveAsync(CurrentProject, token);
            }
            catch (OperationCanceledException)
            {
            }
        }, token);
    }
}