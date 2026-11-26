using System.Collections.ObjectModel;

namespace Boing.ViewModels;

public sealed partial class OrdersViewModel : ObservableObject
{
    private readonly IProjectStateService _stateService;
    private readonly IOrderCodeGeneratorService _codeGenerator;
    private readonly IDialogService _dialogService;

    public ObservableCollection<OrderCostSummary> OrderSummaries { get; } = [];

    [ObservableProperty] private OrderCostSummary? _selectedOrderSummary;

    [ObservableProperty] private ProductionOrder? _selectedOrder;

    public OrdersViewModel(
        IProjectStateService stateService,
        IOrderCodeGeneratorService codeGenerator,
        IDialogService dialogService)
    {
        _stateService = stateService;
        _codeGenerator = codeGenerator;
        _dialogService = dialogService;
        _stateService.StateChanged += OnStateChanged;
        Refresh();
    }

    partial void OnSelectedOrderSummaryChanged(OrderCostSummary? value)
    {
        SelectedOrder = value is null
            ? null
            : _stateService.CurrentProject.ProductionOrders
                .FirstOrDefault(o => o.Id == value.ProductionOrderId);
    }

    public string GetNextOrderCode() =>
        _codeGenerator.GenerateNextCode(_stateService.CurrentProject.ProductionOrders);

    public void AddOrder(ProductionOrder newOrder)
    {
        _stateService.CurrentProject.ProductionOrders.Add(newOrder);
        _stateService.Recalculate();
    }

    public void UpdateOrder(ProductionOrder updatedOrder)
    {
        var orders = _stateService.CurrentProject.ProductionOrders;
        var index = orders.FindIndex(o => o.Id == updatedOrder.Id);
        if (index >= 0)
        {
            orders[index] = updatedOrder;
            _stateService.Recalculate();
        }
    }

    [RelayCommand]
    public async Task DeleteOrderAsync(ProductionOrder? target)
    {
        if (target is null)
        {
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Eliminar orden de producción",
            $"¿Deseas eliminar la orden '{target.Code}' junto con sus consumos de materia prima?",
            "Eliminar",
            "Cancelar");

        if (!confirmed)
        {
            return;
        }

        var project = _stateService.CurrentProject;
        project.ProductionOrders.RemoveAll(o => o.Id == target.Id);
        project.Consumptions.RemoveAll(c => c.ProductionOrderId == target.Id);
        _stateService.Recalculate();
    }

    private void OnStateChanged(object? sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        var previousOrderId = SelectedOrderSummary?.ProductionOrderId;

        OrderSummaries.Clear();
        foreach (var summary in _stateService.CurrentResult.OrderSummaries)
        {
            OrderSummaries.Add(summary);
        }

        var nextSelection = previousOrderId is not null
            ? OrderSummaries.FirstOrDefault(o => o.ProductionOrderId == previousOrderId)
            : null;

        SelectedOrderSummary = nextSelection ?? OrderSummaries.FirstOrDefault();
    }
}