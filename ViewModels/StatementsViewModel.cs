using System.Globalization;

namespace Boing.ViewModels;

public sealed partial class StatementsViewModel : ObservableObject
{
    private readonly IProjectStateService _stateService;
    private readonly WarehouseViewModel _warehouseViewModel;
    private readonly OrdersViewModel _ordersViewModel;
    private readonly ProrationViewModel _prorationViewModel;

    public StatementSettings Settings => _stateService.CurrentProject.StatementSettings;

    public event EventHandler? HeaderChanged;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EcpvVisibility))]
    [NotifyPropertyChangedFor(nameof(ErVisibility))]
    [NotifyPropertyChangedFor(nameof(PageTitle))]
    public partial string? SectionFilter { get; set; }

    public Visibility EcpvVisibility => SectionFilter is null or "Ecpv" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ErVisibility => SectionFilter is null or "Er" ? Visibility.Visible : Visibility.Collapsed;

    public string PageTitle => SectionFilter switch
    {
        "Ecpv" => "Estado de costo de producción y de lo vendido",
        "Er" => "Estado de resultados",
        _ => "Estados financieros"
    };

    public string PageSubtitle
    {
        get
        {
            var method = _warehouseViewModel.CurrentValuationMethod switch
            {
                ValuationMethod.Fifo => "PEPS",
                ValuationMethod.Lifo => "UEPS",
                ValuationMethod.WeightedAverage => "Promedio",
                _ => string.Empty
            };

            return $"Cía. {Settings.CompanyName} S.A. · Valuación {method}";
        }
    }

    [ObservableProperty] public partial string CompanyHeaderText { get; private set; } = string.Empty;

    [ObservableProperty] public partial string PeriodHeaderText { get; private set; } = string.Empty;

    [ObservableProperty] public partial string PreparedByDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string AuthorizedByDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string MaterialDescription { get; private set; } = string.Empty;

    [ObservableProperty] public partial string IncurredDescription { get; private set; } = string.Empty;

    [ObservableProperty] public partial string ProcessDescription { get; private set; } = string.Empty;

    [ObservableProperty] public partial string SoldDescription { get; private set; } = string.Empty;

    [ObservableProperty] public partial string GrossProfitDescription { get; private set; } = string.Empty;

    [ObservableProperty] public partial string OperatingIncomeDescription { get; private set; } = string.Empty;

    [ObservableProperty] public partial string InitialDirectMaterialDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string DirectMaterialPurchasesDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string AvailableDirectMaterialDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string FinalDirectMaterialDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string UsedDirectMaterialDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string DirectLaborDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string OverheadDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string IncurredCostDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string InitialWipDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string TotalProcessedDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string FinalWipDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string FinishedProductionDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string InitialFgDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string AvailableFgDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string FinalFgDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string CostOfGoodsSoldDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string SalesDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string GrossProfitHeader { get; private set; } = "Utilidad bruta";

    [ObservableProperty] public partial string GrossProfitDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string SellingExpensesDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string AdminExpensesDisplay { get; private set; } = "—";

    [ObservableProperty] public partial string OperatingIncomeHeader { get; private set; } = "Utilidad de operación";

    [ObservableProperty] public partial string OperatingIncomeDisplay { get; private set; } = "—";

    public StatementsViewModel(
        IProjectStateService stateService,
        WarehouseViewModel warehouseViewModel,
        OrdersViewModel ordersViewModel,
        ProrationViewModel prorationViewModel)
    {
        _stateService = stateService;
        _warehouseViewModel = warehouseViewModel;
        _ordersViewModel = ordersViewModel;
        _prorationViewModel = prorationViewModel;

        _stateService.StateChanged += (_, _) => RecalculateStatements();
        RecalculateStatements();
    }

    public void SetFilter(string? filter)
    {
        SectionFilter = filter;
        HeaderChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateMetadata(string companyName, DateOnly periodStart, DateOnly periodEnd, string preparedBy,
        string authorizedBy)
    {
        Settings.CompanyName = companyName.Trim();
        Settings.PeriodStart = periodStart;
        Settings.PeriodEnd = periodEnd;
        Settings.PreparedBy = preparedBy.Trim();
        Settings.AuthorizedBy = authorizedBy.Trim();

        _stateService.Recalculate();
    }

    public void UpdateParameters(decimal initialFinishedGoodsCost, decimal sellingExpenses, decimal adminExpenses)
    {
        Settings.InitialFinishedGoodsCost = initialFinishedGoodsCost;
        Settings.SellingExpenses = sellingExpenses;
        Settings.AdminExpenses = adminExpenses;

        _stateService.Recalculate();
    }

    public void AddPreparedSignatureStroke(List<SignaturePoint> stroke)
    {
        if (stroke.Count == 0)
        {
            return;
        }

        Settings.PreparedSignatureStrokes.Add(stroke);
        _stateService.Recalculate();
    }

    public void AddAuthorizedSignatureStroke(List<SignaturePoint> stroke)
    {
        if (stroke.Count == 0)
        {
            return;
        }

        Settings.AuthorizedSignatureStrokes.Add(stroke);
        _stateService.Recalculate();
    }

    public void ClearPreparedSignature()
    {
        Settings.PreparedSignatureStrokes.Clear();
        _stateService.Recalculate();
    }

    public void ClearAuthorizedSignature()
    {
        Settings.AuthorizedSignatureStrokes.Clear();
        _stateService.Recalculate();
    }

    public void RecalculateStatements()
    {
        var settings = Settings;
        var esMx = CultureInfo.GetCultureInfo("es-MX");

        CompanyHeaderText = $"Cía. {settings.CompanyName} S.A.";
        PeriodHeaderText =
            $"Por el periodo del {settings.PeriodStart.ToString("dd 'de' MMMM 'de' yyyy", esMx)} al {settings.PeriodEnd.ToString("dd 'de' MMMM 'de' yyyy", esMx)}";
        PreparedByDisplay = string.IsNullOrWhiteSpace(settings.PreparedBy) ? "—" : settings.PreparedBy;
        AuthorizedByDisplay = string.IsNullOrWhiteSpace(settings.AuthorizedBy) ? "—" : settings.AuthorizedBy;

        var directCards = _warehouseViewModel.Cards
            .Where(c => c.Classification == MaterialClassification.Direct)
            .ToList();

        var initialDirectMaterial = directCards.Sum(c => c.Rows
            .Where(r => r.Date == DateOnly.MinValue)
            .Sum(r => r.Debit));

        var directMaterialPurchases = directCards.Sum(c => c.Rows
            .Where(r => r.Date != DateOnly.MinValue && r.IsPurchase)
            .Sum(r => r.Debit));

        var availableDirectMaterial = initialDirectMaterial + directMaterialPurchases;
        var finalDirectMaterial = directCards.Sum(c => c.FinalInventoryCost);
        var usedDirectMaterial = availableDirectMaterial - finalDirectMaterial;

        var summaries = _ordersViewModel.OrderSummaries.ToList();
        var directLabor = summaries.Sum(s => s.DirectLaborCost);
        var overhead = _prorationViewModel.TotalOverheadApplied;
        var incurredCost = usedDirectMaterial + directLabor + overhead;

        var initialWip = summaries.Sum(s => s.InitialWorkInProcessCost);
        var totalProcessed = incurredCost + initialWip;
        var finalWip = summaries
            .Where(s => s.Status == OrderStatus.InProcess)
            .Sum(s => s.TotalProductionCost);
        var finishedProductionCost = totalProcessed - finalWip;

        var availableFg = finishedProductionCost + settings.InitialFinishedGoodsCost;
        var costOfGoodsSold = summaries
            .Where(s => s.Status == OrderStatus.Finished)
            .Sum(s => s.CostOfGoodsSold);
        var finalFg = Math.Max(0m, availableFg - costOfGoodsSold);

        var totalSales = summaries
            .Where(s => s.Status == OrderStatus.Finished)
            .Sum(s => s.TotalSalesRevenue);
        var grossProfit = totalSales - costOfGoodsSold;
        var operatingIncome = grossProfit - settings.SellingExpenses - settings.AdminExpenses;

        MaterialDescription = $"Materia prima directa utilizada en el periodo: {FormatAmount(usedDirectMaterial)}.";
        IncurredDescription = $"Costo incurrido del periodo: {FormatAmount(incurredCost)}.";
        ProcessDescription = $"Costo de producción de artículos terminados: {FormatAmount(finishedProductionCost)}.";
        SoldDescription = $"Costo de producción de lo vendido: {FormatAmount(costOfGoodsSold)}.";
        GrossProfitDescription = grossProfit >= 0m
            ? $"Utilidad bruta del periodo: {FormatAmount(grossProfit)}."
            : $"Pérdida bruta del periodo: {FormatAmount(Math.Abs(grossProfit))}.";
        OperatingIncomeDescription = operatingIncome >= 0m
            ? $"Utilidad de operación: {FormatAmount(operatingIncome)}."
            : $"Pérdida de operación: {FormatAmount(Math.Abs(operatingIncome))}.";

        InitialDirectMaterialDisplay = FormatAmount(initialDirectMaterial);
        DirectMaterialPurchasesDisplay = FormatAmount(directMaterialPurchases);
        AvailableDirectMaterialDisplay = FormatAmount(availableDirectMaterial);
        FinalDirectMaterialDisplay = FormatAmount(finalDirectMaterial);
        UsedDirectMaterialDisplay = FormatAmount(usedDirectMaterial);

        DirectLaborDisplay = FormatAmount(directLabor);
        OverheadDisplay = FormatAmount(overhead);
        IncurredCostDisplay = FormatAmount(incurredCost);

        InitialWipDisplay = FormatAmount(initialWip);
        TotalProcessedDisplay = FormatAmount(totalProcessed);
        FinalWipDisplay = FormatAmount(finalWip);
        FinishedProductionDisplay = FormatAmount(finishedProductionCost);

        InitialFgDisplay = FormatAmount(settings.InitialFinishedGoodsCost);
        AvailableFgDisplay = FormatAmount(availableFg);
        FinalFgDisplay = FormatAmount(finalFg);
        CostOfGoodsSoldDisplay = FormatAmount(costOfGoodsSold);

        SalesDisplay = FormatAmount(totalSales);
        GrossProfitHeader = grossProfit >= 0m ? "Utilidad bruta" : "Pérdida bruta";
        GrossProfitDisplay = FormatAmount(Math.Abs(grossProfit));

        SellingExpensesDisplay = FormatAmount(settings.SellingExpenses);
        AdminExpensesDisplay = FormatAmount(settings.AdminExpenses);
        OperatingIncomeHeader = operatingIncome >= 0m ? "Utilidad de operación" : "Pérdida de operación";
        OperatingIncomeDisplay = FormatAmount(Math.Abs(operatingIncome));

        OnPropertyChanged(nameof(PageSubtitle));
        HeaderChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string FormatAmount(decimal value) => value == 0m ? "—" : $"{value:C2}";
}