using System.Collections.ObjectModel;

namespace Boing.Views;

public sealed partial class WarehousePage : IDynamicCommandPage
{
    private readonly IProjectStateService _stateService;
    private readonly IDialogService _dialogService;
    private readonly HashSet<string> _selectedMaterialIds = [];
    private bool _isSelectionMode;
    private MaterialClassification? _currentFilter;

    public WarehouseViewModel ViewModel { get; }
    public ObservableCollection<WarehouseCardDisplayItem> DisplayCards { get; } = [];

    public string PageTitle
    {
        get
        {
            if (!_isSelectionMode)
            {
                return "Tarjetas de almacén";
            }

            if (_selectedMaterialIds.Count == 0)
            {
                return "Seleccionar tarjetas";
            }

            return _selectedMaterialIds.Count == 1
                ? "1 tarjeta seleccionada"
                : $"{_selectedMaterialIds.Count} tarjetas seleccionadas";
        }
    }

    public string PageSubtitle
    {
        get
        {
            if (_isSelectionMode)
            {
                return "Tarjetas de almacén";
            }

            var directCount = ViewModel.Cards.Count(c => c.Classification == MaterialClassification.Direct);
            var indirectCount = ViewModel.Cards.Count(c => c.Classification == MaterialClassification.Indirect);
            var method = ViewModel.CurrentValuationMethod switch
            {
                ValuationMethod.Fifo => "PEPS",
                ValuationMethod.Lifo => "UEPS",
                ValuationMethod.WeightedAverage => "Promedio",
                _ => string.Empty
            };

            var filterLabel = _currentFilter switch
            {
                MaterialClassification.Direct => " · Solo directos",
                MaterialClassification.Indirect => " · Solo indirectos",
                _ => string.Empty
            };

            return $"{directCount} ingredientes directos, {indirectCount} indirectos · {method}{filterLabel}";
        }
    }

    public string PageIconGlyph => "\uE8F1";

    public event EventHandler? HeaderStateChanged;

    public WarehousePage()
    {
        InitializeComponent();
        ViewModel = AppServices.GetService<WarehouseViewModel>();
        _stateService = AppServices.GetService<IProjectStateService>();
        _dialogService = AppServices.GetService<IDialogService>();

        ViewModel.Cards.CollectionChanged += (_, _) =>
        {
            RebuildDisplayCards();
            NotifyHeaderChanged();
        };

        RebuildDisplayCards();
    }

    public IReadOnlyList<ICommandBarElement> GetDynamicCommands()
    {
        List<ICommandBarElement> commands = [];

        if (!_isSelectionMode)
        {
            var selectButton = new AppBarButton
            {
                Label = "Seleccionar tarjetas",
                Icon = new FontIcon { Glyph = "\uE9D9" }
            };
            selectButton.Click += OnEnterSelectionModeClick;
            commands.Add(selectButton);
        }
        else
        {
            var cancelButton = new AppBarButton
            {
                Label = "Cancelar",
                Icon = new FontIcon { Glyph = "\uE711" }
            };
            cancelButton.Click += OnCancelSelectionModeClick;
            commands.Add(cancelButton);

            if (_selectedMaterialIds.Count > 0)
            {
                var deleteButton = new AppBarButton
                {
                    Label = "Eliminar",
                    Icon = new FontIcon { Glyph = "\uE74D" }
                };
                deleteButton.Click += OnDeleteSelectedClick;
                commands.Add(deleteButton);
            }
        }

        var addButton = new AppBarButton
        {
            Label = "Agregar ingrediente",
            Icon = new FontIcon { Glyph = "\uE710" }
        };
        addButton.Click += OnAddCardClick;
        commands.Add(addButton);

        var valuationButton = new AppBarButton
        {
            Label = "Valuación",
            Icon = new FontIcon { Glyph = "\uE7C1", HorizontalAlignment = HorizontalAlignment.Center },
            Flyout = BuildValuationFlyout()
        };
        commands.Add(valuationButton);

        var filterButton = new AppBarButton
        {
            Label = "Filtrado",
            Icon = new FontIcon { Glyph = "\uE71C" },
            Flyout = BuildFilterFlyout()
        };
        commands.Add(filterButton);

        return commands;
    }

    private void RebuildDisplayCards()
    {
        DisplayCards.Clear();

        var filtered = _currentFilter is null
            ? ViewModel.Cards
            : ViewModel.Cards.Where(c => c.Classification == _currentFilter.Value);

        foreach (var card in filtered)
        {
            var purchasesCount = _stateService.CurrentProject.Purchases
                .Count(p => p.MaterialId == card.MaterialId);
            var consumptionsCount = _stateService.CurrentProject.Consumptions
                .Count(c => c.MaterialId == card.MaterialId);

            var isDirect = card.Classification == MaterialClassification.Direct;
            var typeLabel = isDirect ? "Ingrediente directo" : "Ingrediente indirecto";
            var currentBalanceUnits = card.Rows.Count > 0 ? card.Rows[^1].BalanceQuantity : 0m;
            var purchasesLabel = purchasesCount == 1 ? "1 compra" : $"{purchasesCount} compras";
            var consumptionsLabel = consumptionsCount == 1 ? "1 consumo" : $"{consumptionsCount} consumos";

            var rowItems = card.Rows.Select((row, index) =>
            {
                var isInitial = row.Date == DateOnly.MinValue;
                var isEntry = row.EntryQuantity > 0m;
                var isExit = row.ExitQuantity > 0m;

                return new WarehouseRowDisplayItem
                {
                    MaterialId = card.MaterialId,
                    MovementId = row.MovementId,
                    IsPurchase = row.IsPurchase,
                    IsInitialBalance = isInitial,
                    DateDisplay = isInitial ? "Inicial" : row.Date.ToString("dd/MM/yyyy"),
                    RefCode = (index + 1).ToString("D3"),
                    Concept = row.Concept,
                    EntryUnitsDisplay = isEntry ? $"{row.EntryQuantity:N2}" : "—",
                    ExitUnitsDisplay = isExit ? $"{row.ExitQuantity:N2}" : "—",
                    BalanceUnitsDisplay = $"{row.BalanceQuantity:N2}",
                    EntryUnitCostDisplay = isEntry ? $"{row.UnitCost:C2}" : "—",
                    ExitUnitCostDisplay = isExit ? $"{row.UnitCost:C2}" : "—",
                    DebitDisplay = row.Debit > 0m ? $"{row.Debit:C2}" : "—",
                    CreditDisplay = row.Credit > 0m ? $"{row.Credit:C2}" : "—",
                    MonetaryBalanceDisplay = $"{row.MonetaryBalance:C2}"
                };
            }).ToList();

            DisplayCards.Add(new WarehouseCardDisplayItem
            {
                MaterialId = card.MaterialId,
                MaterialName = card.MaterialName,
                UnitsHeader = $"Unidades ({card.Unit})",
                SummaryDescription =
                    $"{typeLabel} · {purchasesLabel} y {consumptionsLabel} registrados. Existencia actual: {currentBalanceUnits:N2} {card.Unit} con saldo valuado en {card.FinalInventoryCost:C2}.",
                IsSelected = _selectedMaterialIds.Contains(card.MaterialId),
                SelectionVisibility = _isSelectionMode ? Visibility.Visible : Visibility.Collapsed,
                Rows = rowItems
            });
        }
    }

    private void OnRowItemClick(object sender, ItemClickEventArgs e)
    {
        if (sender is not ListView list || e.ClickedItem is not WarehouseRowDisplayItem rowItem)
        {
            return;
        }

        var container = list.ContainerFromItem(rowItem);
        if (container is not FrameworkElement targetElement)
        {
            return;
        }

        var flyout = new MenuFlyout();

        var editItem = new MenuFlyoutItem
        {
            Text = rowItem.IsInitialBalance ? "Editar ingrediente" : "Editar movimiento",
            Icon = new FontIcon { Glyph = "\uE70F" }
        };
        editItem.Click += async (_, _) => await OpenEditRowDialogAsync(rowItem);
        flyout.Items.Add(editItem);

        var deleteItem = new MenuFlyoutItem
        {
            Text = "Eliminar movimiento",
            Icon = new FontIcon { Glyph = "\uE74D" },
            IsEnabled = !rowItem.IsInitialBalance
        };
        deleteItem.Click += async (_, _) => await DeleteRowAsync(rowItem);
        flyout.Items.Add(deleteItem);

        flyout.ShowAt(targetElement);
    }

    private void OnCardCheckBoxClick(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox box || box.Tag is not string materialId)
        {
            return;
        }

        var isNowSelected = _selectedMaterialIds.Add(materialId);
        if (!isNowSelected)
        {
            _selectedMaterialIds.Remove(materialId);
        }

        var targetCard = DisplayCards.FirstOrDefault(c => c.MaterialId == materialId);
        if (targetCard is not null)
        {
            targetCard.IsSelected = isNowSelected;
        }

        NotifyHeaderChanged();
    }

    private async void OnAddOperationClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button actionButton || actionButton.Tag is not string materialId)
        {
            return;
        }

        await OpenAddOperationDialogAsync(materialId);
    }

    private async Task OpenAddOperationDialogAsync(string materialId)
    {
        var targetMaterial = _stateService.CurrentProject.Materials
            .FirstOrDefault(m => m.Id == materialId);

        if (targetMaterial is null)
        {
            return;
        }

        var dialog = new MovementDialog(targetMaterial, _stateService.CurrentProject.ProductionOrders);
        await _dialogService.ShowDialogAsync(dialog);

        if (!dialog.ViewModel.IsOperationSuccessful)
        {
            return;
        }

        if (dialog.ViewModel.ResultPurchase is not null)
        {
            ViewModel.AddPurchase(dialog.ViewModel.ResultPurchase);
        }
        else if (dialog.ViewModel.ResultConsumption is not null)
        {
            ViewModel.AddConsumption(dialog.ViewModel.ResultConsumption);
        }
    }

    private async Task OpenEditRowDialogAsync(WarehouseRowDisplayItem rowItem)
    {
        var targetMaterial = _stateService.CurrentProject.Materials
            .FirstOrDefault(m => m.Id == rowItem.MaterialId);

        if (targetMaterial is null)
        {
            return;
        }

        if (rowItem.IsInitialBalance)
        {
            var dialog = new MaterialDialog(targetMaterial);
            await _dialogService.ShowDialogAsync(dialog);

            if (dialog.ViewModel.IsOperationSuccessful && dialog.ViewModel.ResultMaterial is not null)
            {
                ViewModel.UpdateMaterial(dialog.ViewModel.ResultMaterial);
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(rowItem.MovementId))
        {
            return;
        }

        if (rowItem.IsPurchase)
        {
            var existingPurchase = _stateService.CurrentProject.Purchases
                .FirstOrDefault(p => p.Id == rowItem.MovementId);

            if (existingPurchase is null)
            {
                return;
            }

            var dialog = new MovementDialog(
                targetMaterial,
                _stateService.CurrentProject.ProductionOrders,
                existingPurchase: existingPurchase);

            await _dialogService.ShowDialogAsync(dialog);

            if (dialog.ViewModel.IsOperationSuccessful && dialog.ViewModel.ResultPurchase is not null)
            {
                ViewModel.UpdatePurchase(dialog.ViewModel.ResultPurchase);
            }
        }
        else
        {
            var existingConsumption = _stateService.CurrentProject.Consumptions
                .FirstOrDefault(c => c.Id == rowItem.MovementId);

            if (existingConsumption is null)
            {
                return;
            }

            var dialog = new MovementDialog(
                targetMaterial,
                _stateService.CurrentProject.ProductionOrders,
                existingConsumption: existingConsumption);

            await _dialogService.ShowDialogAsync(dialog);

            if (dialog.ViewModel.IsOperationSuccessful && dialog.ViewModel.ResultConsumption is not null)
            {
                ViewModel.UpdateConsumption(dialog.ViewModel.ResultConsumption);
            }
        }
    }

    private async Task DeleteRowAsync(WarehouseRowDisplayItem rowItem)
    {
        if (rowItem.IsInitialBalance || string.IsNullOrWhiteSpace(rowItem.MovementId))
        {
            return;
        }

        if (rowItem.IsPurchase)
        {
            var target = _stateService.CurrentProject.Purchases
                .FirstOrDefault(p => p.Id == rowItem.MovementId);
            await ViewModel.DeletePurchaseAsync(target);
        }
        else
        {
            var target = _stateService.CurrentProject.Consumptions
                .FirstOrDefault(c => c.Id == rowItem.MovementId);
            await ViewModel.DeleteConsumptionAsync(target);
        }
    }

    private MenuFlyout BuildValuationFlyout()
    {
        var flyout = new MenuFlyout();

        flyout.Items.Add(CreateValuationItem("PEPS", ValuationMethod.Fifo));
        flyout.Items.Add(CreateValuationItem("UEPS", ValuationMethod.Lifo));
        flyout.Items.Add(CreateValuationItem("Costo promedio", ValuationMethod.WeightedAverage));

        return flyout;
    }

    private ToggleMenuFlyoutItem CreateValuationItem(string text, ValuationMethod method)
    {
        var item = new ToggleMenuFlyoutItem
        {
            Text = text,
            IsChecked = ViewModel.CurrentValuationMethod == method
        };

        item.Click += (_, _) =>
        {
            ViewModel.SetValuationMethodCommand.Execute(method);
            RebuildDisplayCards();
            NotifyHeaderChanged();
        };

        return item;
    }

    private MenuFlyout BuildFilterFlyout()
    {
        var flyout = new MenuFlyout();

        flyout.Items.Add(CreateFilterItem("Todos", null));
        flyout.Items.Add(CreateFilterItem("Solo directos", MaterialClassification.Direct));
        flyout.Items.Add(CreateFilterItem("Solo indirectos", MaterialClassification.Indirect));

        return flyout;
    }

    private ToggleMenuFlyoutItem CreateFilterItem(string text, MaterialClassification? filter)
    {
        var item = new ToggleMenuFlyoutItem
        {
            Text = text,
            IsChecked = _currentFilter == filter
        };

        item.Click += (_, _) =>
        {
            _currentFilter = filter;
            RebuildDisplayCards();
            NotifyHeaderChanged();
        };

        return item;
    }

    private void OnEnterSelectionModeClick(object sender, RoutedEventArgs e)
    {
        _isSelectionMode = true;
        _selectedMaterialIds.Clear();

        foreach (var card in DisplayCards)
        {
            card.IsSelected = false;
            card.SelectionVisibility = Visibility.Visible;
        }

        NotifyHeaderChanged();
    }

    private void OnCancelSelectionModeClick(object sender, RoutedEventArgs e)
    {
        _isSelectionMode = false;
        _selectedMaterialIds.Clear();

        foreach (var card in DisplayCards)
        {
            card.IsSelected = false;
            card.SelectionVisibility = Visibility.Collapsed;
        }

        NotifyHeaderChanged();
    }

    private async void OnDeleteSelectedClick(object sender, RoutedEventArgs e)
    {
        if (_selectedMaterialIds.Count == 0)
        {
            return;
        }

        var count = _selectedMaterialIds.Count;
        var message = count == 1
            ? "¿Eliminar la tarjeta seleccionada junto con todas sus compras y consumos?"
            : $"¿Eliminar las {count} tarjetas seleccionadas junto con todas sus compras y consumos?";

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Eliminar tarjetas",
            message,
            "Eliminar",
            "Cancelar");

        if (!confirmed)
        {
            return;
        }

        ViewModel.DeleteMaterials(_selectedMaterialIds.ToList());
        _selectedMaterialIds.Clear();
        _isSelectionMode = false;
        _stateService.Recalculate();
    }

    private async void OnAddCardClick(object sender, RoutedEventArgs e)
    {
        var dialog = new MaterialDialog();
        await _dialogService.ShowDialogAsync(dialog);

        if (dialog.ViewModel.IsOperationSuccessful && dialog.ViewModel.ResultMaterial is not null)
        {
            ViewModel.AddMaterial(dialog.ViewModel.ResultMaterial);
        }
    }

    private void NotifyHeaderChanged() =>
        HeaderStateChanged?.Invoke(this, EventArgs.Empty);
}

public sealed partial class WarehouseCardDisplayItem : ObservableObject
{
    public string MaterialId { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string UnitsHeader { get; set; } = string.Empty;
    public string SummaryDescription { get; set; } = string.Empty;
    public List<WarehouseRowDisplayItem> Rows { get; set; } = [];

    [ObservableProperty] private bool _isSelected;

    [ObservableProperty] private Visibility _selectionVisibility = Visibility.Collapsed;
}

public sealed class WarehouseRowDisplayItem
{
    public string MaterialId { get; set; } = string.Empty;
    public string? MovementId { get; set; }
    public bool IsPurchase { get; set; }
    public bool IsInitialBalance { get; set; }
    public string DateDisplay { get; set; } = string.Empty;
    public string RefCode { get; set; } = string.Empty;
    public string Concept { get; set; } = string.Empty;
    public string EntryUnitsDisplay { get; set; } = string.Empty;
    public string ExitUnitsDisplay { get; set; } = string.Empty;
    public string BalanceUnitsDisplay { get; set; } = string.Empty;
    public string EntryUnitCostDisplay { get; set; } = string.Empty;
    public string ExitUnitCostDisplay { get; set; } = string.Empty;
    public string DebitDisplay { get; set; } = string.Empty;
    public string CreditDisplay { get; set; } = string.Empty;
    public string MonetaryBalanceDisplay { get; set; } = string.Empty;
}