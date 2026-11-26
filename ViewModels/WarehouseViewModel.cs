using System.Collections.ObjectModel;

namespace Boing.ViewModels;

public sealed partial class WarehouseViewModel : ObservableObject
{
    private readonly IProjectStateService _stateService;
    private readonly IDialogService _dialogService;

    public ObservableCollection<WarehouseCardResult> Cards { get; } = [];
    public ObservableCollection<WarehouseCardRow> SelectedCardRows { get; } = [];
    public ObservableCollection<Purchase> SelectedMaterialPurchases { get; } = [];
    public ObservableCollection<Consumption> SelectedMaterialConsumptions { get; } = [];

    [ObservableProperty] private WarehouseCardResult? _selectedCard;

    [ObservableProperty] private Material? _selectedMaterial;

    [ObservableProperty] private ValuationMethod _currentValuationMethod = ValuationMethod.Fifo;

    public WarehouseViewModel(IProjectStateService stateService, IDialogService dialogService)
    {
        _stateService = stateService;
        _dialogService = dialogService;
        _stateService.StateChanged += OnStateChanged;
        Refresh();
    }

    partial void OnSelectedCardChanged(WarehouseCardResult? value) =>
        LoadSelectedCardDetails(value);

    [RelayCommand]
    private void SetValuationMethod(ValuationMethod method)
    {
        if (_stateService.CurrentProject.ValuationMethod == method)
        {
            return;
        }

        _stateService.CurrentProject.ValuationMethod = method;
        _stateService.Recalculate();
    }

    public void AddMaterial(Material newMaterial)
    {
        _stateService.CurrentProject.Materials.Add(newMaterial);
        _stateService.Recalculate();
    }

    public void UpdateMaterial(Material updatedMaterial)
    {
        var materials = _stateService.CurrentProject.Materials;
        var index = materials.FindIndex(m => m.Id == updatedMaterial.Id);
        if (index >= 0)
        {
            materials[index] = updatedMaterial;
            _stateService.Recalculate();
        }
    }

    [RelayCommand]
    public async Task DeleteMaterialAsync(Material? target)
    {
        if (target is null)
        {
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Eliminar ingrediente",
            $"¿Deseas eliminar '{target.Name}' junto con todas sus compras y consumos?",
            "Eliminar",
            "Cancelar");

        if (!confirmed)
        {
            return;
        }

        var project = _stateService.CurrentProject;
        project.Materials.RemoveAll(m => m.Id == target.Id);
        project.Purchases.RemoveAll(p => p.MaterialId == target.Id);
        project.Consumptions.RemoveAll(c => c.MaterialId == target.Id);
        _stateService.Recalculate();
    }

    public void AddPurchase(Purchase newPurchase)
    {
        _stateService.CurrentProject.Purchases.Add(newPurchase);
        _stateService.Recalculate();
    }

    public void UpdatePurchase(Purchase updatedPurchase)
    {
        var purchases = _stateService.CurrentProject.Purchases;
        var index = purchases.FindIndex(p => p.Id == updatedPurchase.Id);
        if (index >= 0)
        {
            purchases[index] = updatedPurchase;
            _stateService.Recalculate();
        }
    }

    [RelayCommand]
    public async Task DeletePurchaseAsync(Purchase? target)
    {
        if (target is null)
        {
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Eliminar compra",
            "¿Deseas eliminar esta compra de la tarjeta de almacén?",
            "Eliminar",
            "Cancelar");

        if (!confirmed)
        {
            return;
        }

        _stateService.CurrentProject.Purchases.RemoveAll(p => p.Id == target.Id);
        _stateService.Recalculate();
    }

    public void AddConsumption(Consumption newConsumption)
    {
        _stateService.CurrentProject.Consumptions.Add(newConsumption);
        _stateService.Recalculate();
    }

    public void UpdateConsumption(Consumption updatedConsumption)
    {
        var consumptions = _stateService.CurrentProject.Consumptions;
        var index = consumptions.FindIndex(c => c.Id == updatedConsumption.Id);
        if (index >= 0)
        {
            consumptions[index] = updatedConsumption;
            _stateService.Recalculate();
        }
    }

    [RelayCommand]
    public async Task DeleteConsumptionAsync(Consumption? target)
    {
        if (target is null)
        {
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Eliminar consumo",
            "¿Deseas eliminar este consumo de la tarjeta de almacén?",
            "Eliminar",
            "Cancelar");

        if (!confirmed)
        {
            return;
        }

        _stateService.CurrentProject.Consumptions.RemoveAll(c => c.Id == target.Id);
        _stateService.Recalculate();
    }

    public void DeleteMaterials(IReadOnlyCollection<string> materialIds)
    {
        if (materialIds.Count == 0)
        {
            return;
        }

        var project = _stateService.CurrentProject;
        project.Materials.RemoveAll(m => materialIds.Contains(m.Id));
        project.Purchases.RemoveAll(p => materialIds.Contains(p.MaterialId));
        project.Consumptions.RemoveAll(c => materialIds.Contains(c.MaterialId));
        _stateService.Recalculate();
    }

    private void OnStateChanged(object? sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        CurrentValuationMethod = _stateService.CurrentProject.ValuationMethod;
        var previousMaterialId = SelectedCard?.MaterialId;

        Cards.Clear();
        foreach (var card in _stateService.CurrentResult.WarehouseCards)
        {
            Cards.Add(card);
        }

        var nextSelection = previousMaterialId is not null
            ? Cards.FirstOrDefault(c => c.MaterialId == previousMaterialId)
            : null;

        SelectedCard = nextSelection ?? Cards.FirstOrDefault();
        LoadSelectedCardDetails(SelectedCard);
    }

    private void LoadSelectedCardDetails(WarehouseCardResult? card)
    {
        SelectedCardRows.Clear();
        SelectedMaterialPurchases.Clear();
        SelectedMaterialConsumptions.Clear();

        if (card is null)
        {
            SelectedMaterial = null;
            return;
        }

        SelectedMaterial = _stateService.CurrentProject.Materials
            .FirstOrDefault(m => m.Id == card.MaterialId);

        foreach (var row in card.Rows)
        {
            SelectedCardRows.Add(row);
        }

        foreach (var item in _stateService.CurrentProject.Purchases
                     .Where(p => p.MaterialId == card.MaterialId)
                     .OrderBy(p => p.Date))
        {
            SelectedMaterialPurchases.Add(item);
        }

        foreach (var item in _stateService.CurrentProject.Consumptions
                     .Where(c => c.MaterialId == card.MaterialId)
                     .OrderBy(c => c.Date))
        {
            SelectedMaterialConsumptions.Add(item);
        }
    }
}