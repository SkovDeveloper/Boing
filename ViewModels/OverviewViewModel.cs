namespace Boing.ViewModels;

public sealed partial class OverviewViewModel : ObservableObject
{
    private readonly IProjectStateService _stateService;

    public IReadOnlyList<ValuationMethod> ValuationMethods { get; } = Enum.GetValues<ValuationMethod>();
    public IReadOnlyList<PricingMode> PricingModes { get; } = Enum.GetValues<PricingMode>();

    [ObservableProperty] private string _productName = string.Empty;

    [ObservableProperty] private string _periodName = string.Empty;

    [ObservableProperty] private ValuationMethod _selectedValuationMethod = ValuationMethod.Fifo;

    [ObservableProperty] private PricingMode _selectedPricingMode = PricingMode.MarkupPercentage;

    [ObservableProperty] private decimal _pricingValue;

    [ObservableProperty] private decimal _sellingExpenses;

    [ObservableProperty] private decimal _administrativeExpenses;

    [ObservableProperty] private decimal _initialFinishedGoodsUnits;

    [ObservableProperty] private decimal _initialFinishedGoodsUnitCost;

    [ObservableProperty] private decimal _totalProductionCost;

    [ObservableProperty] private decimal _totalSales;

    [ObservableProperty] private decimal _grossProfit;

    [ObservableProperty] private decimal _operatingIncome;

    [ObservableProperty] private decimal _averageUnitCost;

    [ObservableProperty] private int _finishedOrdersCount;

    [ObservableProperty] private int _inProcessOrdersCount;

    public OverviewViewModel(IProjectStateService stateService)
    {
        _stateService = stateService;
        _stateService.StateChanged += OnStateChanged;
        Refresh();
    }

    [RelayCommand]
    private void ApplySettings()
    {
        var project = _stateService.CurrentProject;
        project.ProductName = ProductName;
        project.PeriodName = PeriodName;
        project.ValuationMethod = SelectedValuationMethod;
        project.PricingMode = SelectedPricingMode;
        project.PricingValue = PricingValue;
        project.SellingExpenses = SellingExpenses;
        project.AdministrativeExpenses = AdministrativeExpenses;
        project.InitialFinishedGoodsUnits = InitialFinishedGoodsUnits;
        project.InitialFinishedGoodsUnitCost = InitialFinishedGoodsUnitCost;

        _stateService.Recalculate();
    }

    private void OnStateChanged(object? sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        var project = _stateService.CurrentProject;
        var result = _stateService.CurrentResult;

        ProductName = project.ProductName;
        PeriodName = project.PeriodName;
        SelectedValuationMethod = project.ValuationMethod;
        SelectedPricingMode = project.PricingMode;
        PricingValue = project.PricingValue;
        SellingExpenses = project.SellingExpenses;
        AdministrativeExpenses = project.AdministrativeExpenses;
        InitialFinishedGoodsUnits = project.InitialFinishedGoodsUnits;
        InitialFinishedGoodsUnitCost = project.InitialFinishedGoodsUnitCost;

        TotalProductionCost = result.CostOfProductionStatement.IncurredProductionCost;
        TotalSales = result.IncomeStatement.TotalSales;
        GrossProfit = result.IncomeStatement.GrossProfit;
        OperatingIncome = result.IncomeStatement.OperatingIncome;

        FinishedOrdersCount = result.OrderSummaries.Count(o => o.Status == OrderStatus.Finished);
        InProcessOrdersCount = result.OrderSummaries.Count(o => o.Status == OrderStatus.InProcess);

        var finished = result.OrderSummaries
            .Where(o => o.Status == OrderStatus.Finished && o.ProducedUnits > 0m)
            .ToList();

        AverageUnitCost = finished.Count > 0
            ? finished.Average(o => o.UnitProductionCost)
            : 0m;
    }
}