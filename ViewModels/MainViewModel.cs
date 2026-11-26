namespace Boing.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IProjectStateService _stateService;
    private readonly IProjectStorageService _storageService;
    private readonly IFilePickerService _filePickerService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private string _productName = string.Empty;

    [ObservableProperty]
    private string _periodName = string.Empty;

    [ObservableProperty]
    private string _valuationMethodLabel = string.Empty;

    public MainViewModel(
        IProjectStateService stateService,
        IProjectStorageService storageService,
        IFilePickerService filePickerService,
        IDialogService dialogService)
    {
        _stateService = stateService;
        _storageService = storageService;
        _filePickerService = filePickerService;
        _dialogService = dialogService;

        _stateService.StateChanged += OnStateChanged;
    }

    [RelayCommand]
    public Task InitializeAsync() => _stateService.InitializeAsync();

    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        var path = await _filePickerService.PickOpenJsonPathAsync();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var loaded = await _storageService.LoadAsync(path);
        if (loaded is null)
        {
            await _dialogService.ShowMessageAsync("Error", "No se pudo cargar el archivo JSON seleccionado.");
            return;
        }

        _stateService.LoadProject(loaded);
    }

    [RelayCommand]
    private async Task SaveProjectAsync()
    {
        var path = await _filePickerService.PickSaveJsonPathAsync("EscenarioBoingCostos");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await _StorageServiceSaveAsync(path);
    }

    private async Task _StorageServiceSaveAsync(string path)
    {
        await _storageService.SaveAsync(_stateService.CurrentProject, path);
        await _dialogService.ShowMessageAsync("Guardado", "El escenario se guardó correctamente.");
    }

    [RelayCommand]
    private async Task ResetToDefaultAsync()
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Restablecer escenario",
            "¿Deseas restaurar el escenario base de Boing de Mango? Se perderán los cambios no guardados.",
            "Restablecer",
            "Cancelar");

        if (!confirmed)
        {
            return;
        }

        var defaultProject = await _storageService.LoadDefaultScenarioAsync();
        _stateService.LoadProject(defaultProject);
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        var project = _stateService.CurrentProject;
        ProductName = project.ProductName;
        PeriodName = project.PeriodName;
        ValuationMethodLabel = project.ValuationMethod switch
        {
            ValuationMethod.Fifo => "PEPS (FIFO)",
            ValuationMethod.Lifo => "UEPS (LIFO)",
            ValuationMethod.WeightedAverage => "Costo promedio",
            _ => string.Empty
        };
    }
}