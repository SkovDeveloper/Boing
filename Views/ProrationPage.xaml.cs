using Microsoft.UI.Xaml.Media;

namespace Boing.Views;

public sealed partial class ProrationPage : IDynamicCommandPage
{
    private readonly IProjectStateService _stateService;
    private readonly IDialogService _dialogService;
    private string? _sectionFilter;

    public ProrationViewModel ViewModel { get; }
    public OrdersViewModel OrdersViewModel { get; }

    public Visibility ProrationsVisibility =>
        _sectionFilter is null or "Prorations" ? Visibility.Visible : Visibility.Collapsed;

    public Visibility CupVisibility => _sectionFilter is null or "Cup" ? Visibility.Visible : Visibility.Collapsed;

    public string PrimaryDescription =>
        $"{ViewModel.PrimaryRows.Count} conceptos indirectos distribuidos entre {ViewModel.Departments.Count} departamentos.";

    public string SecondaryDescription =>
        $"{ViewModel.SecondaryRows.Count} departamentos de servicio cerrados hacia departamentos productivos.";

    public string FinalDescription =>
        $"GIF total aplicado a {OrdersViewModel.OrderSummaries.Count} órdenes de producción: {ViewModel.TotalOverheadApplied:C2}.";

    public string CupDescription
    {
        get
        {
            var finishedCount = OrdersViewModel.OrderSummaries.Count(o => o.Status == OrderStatus.Finished);
            var inProcessCount = OrdersViewModel.OrderSummaries.Count(o => o.Status == OrderStatus.InProcess);
            var ifppTotal = OrdersViewModel.OrderSummaries
                .Where(o => o.Status == OrderStatus.InProcess)
                .Sum(o => o.TotalProductionCost);

            return $"{finishedCount} terminadas, {inProcessCount} en proceso · IFPP valuado en {ifppTotal:C2}.";
        }
    }

    public string PageTitle => "Prorrateos y costo unitario de producción";

    public string PageSubtitle
    {
        get
        {
            var productiveCount = ViewModel.Departments.Count(d => d.Type == DepartmentType.Productive);
            var serviceCount = ViewModel.Departments.Count(d => d.Type == DepartmentType.Service);
            return
                $"{productiveCount} productivos, {serviceCount} de servicio · {OrdersViewModel.OrderSummaries.Count} órdenes · {ViewModel.TotalOverheadApplied:C2} GIF";
        }
    }

    public string PageIconGlyph => "\uE8EF";

    public event EventHandler? HeaderStateChanged;

    public ProrationPage()
    {
        InitializeComponent();
        ViewModel = AppServices.GetService<ProrationViewModel>();
        OrdersViewModel = AppServices.GetService<OrdersViewModel>();
        _stateService = AppServices.GetService<IProjectStateService>();
        _dialogService = AppServices.GetService<IDialogService>();

        _stateService.StateChanged += (_, _) =>
        {
            RebuildAllMatrices();
            Bindings.Update();
            NotifyHeaderChanged();
        };

        RebuildAllMatrices();
    }

    public IReadOnlyList<ICommandBarElement> GetDynamicCommands()
    {
        List<ICommandBarElement> commands = [];

        var addExpenseButton = new AppBarButton
        {
            Label = "Gasto GIF",
            Icon = new FontIcon { Glyph = "\uE710" }
        };
        addExpenseButton.Click += OnAddExpenseClick;
        commands.Add(addExpenseButton);

        var addDepartmentButton = new AppBarButton
        {
            Label = "Departamento",
            Icon = new FontIcon { Glyph = "\uE821" }
        };
        addDepartmentButton.Click += OnAddDepartmentClick;
        commands.Add(addDepartmentButton);

        var addOrderButton = new AppBarButton
        {
            Label = "Orden",
            Icon = new FontIcon { Glyph = "\uE9F9" }
        };
        addOrderButton.Click += OnAddOrderClick;
        commands.Add(addOrderButton);

        var filterButton = new AppBarButton
        {
            Label = "Filtrar",
            Icon = new FontIcon { Glyph = "\uE71C" },
            Flyout = BuildFilterFlyout()
        };
        commands.Add(filterButton);

        return commands;
    }

    private (List<Department> Productive, List<Department> Service, List<Department> Ordered, Dictionary<string, string>
        Aliases) GetOrderedDepartmentsWithAliases()
    {
        var productiveDepts = _stateService.CurrentProject.Departments
            .Where(d => d.Type == DepartmentType.Productive)
            .ToList();
        var serviceDepts = _stateService.CurrentProject.Departments
            .Where(d => d.Type == DepartmentType.Service)
            .ToList();
        var orderedDepts = productiveDepts.Concat(serviceDepts).ToList();

        Dictionary<string, string> aliases = [];
        for (var index = 0; index < orderedDepts.Count; index++)
        {
            aliases[orderedDepts[index].Id] = GetDepartmentAlias(index);
        }

        return (productiveDepts, serviceDepts, orderedDepts, aliases);
    }

    private static string GetDepartmentAlias(int index)
    {
        var alias = string.Empty;
        var current = index;
        while (current >= 0)
        {
            alias = (char)('A' + (current % 26)) + alias;
            current = (current / 26) - 1;
        }

        return alias;
    }

    private void RebuildAllMatrices()
    {
        var (productiveDepts, serviceDepts, orderedDepts, aliases) = GetOrderedDepartmentsWithAliases();

        BuildPrimaryMatrix(productiveDepts, serviceDepts, orderedDepts, aliases);
        BuildSecondaryMatrix(productiveDepts, serviceDepts, orderedDepts, aliases);
        BuildFinalMatrix(productiveDepts, orderedDepts, aliases);
        BuildCupMatrix();
    }

    private void BuildPrimaryMatrix(
        List<Department> productiveDepts,
        List<Department> serviceDepts,
        List<Department> orderedDepts,
        Dictionary<string, string> aliases)
    {
        ResetGrid(PrimaryMatrixGrid);
        ResetGrid(PrimaryTotalGrid);

        SetupDepartmentColumns(PrimaryMatrixGrid, productiveDepts, serviceDepts);
        SetupDepartmentColumns(PrimaryTotalGrid, productiveDepts, serviceDepts);

        AddDepartmentDoubleHeader(PrimaryMatrixGrid, "Concepto", "Base", productiveDepts, serviceDepts, aliases);

        var rowIndex = 2;
        var deptTotals = orderedDepts.ToDictionary(d => d.Id, _ => 0m);
        var grandTotal = 0m;

        foreach (var row in ViewModel.PrimaryRows)
        {
            PrimaryMatrixGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var isIndirectMaterial = row.Concept == "Indirect Raw Material";
            var linkedExpense = isIndirectMaterial
                ? null
                : _stateService.CurrentProject.OverheadExpenses.FirstOrDefault(e => e.Concept == row.Concept);

            var totalColumns = 2 + orderedDepts.Count + 1;
            if (linkedExpense is not null)
            {
                AddClickableRowOverlay(PrimaryMatrixGrid, rowIndex, totalColumns,
                    target => ShowExpenseFlyout(target, linkedExpense));
            }

            var conceptLabel = isIndirectMaterial ? "Materia prima indirecta" : row.Concept;
            var baseLabel = row.AllocationBase switch
            {
                "Direct Assignment" => "Directo",
                "IndirectMaterialBase" => "Base MPI",
                _ => row.AllocationBase
            };

            AddCell(PrimaryMatrixGrid, rowIndex, 0, conceptLabel, HorizontalAlignment.Left);
            AddCell(PrimaryMatrixGrid, rowIndex, 1, baseLabel, HorizontalAlignment.Left, isSecondary: true);

            for (var deptIndex = 0; deptIndex < orderedDepts.Count; deptIndex++)
            {
                var dept = orderedDepts[deptIndex];
                var amount = row.AmountsByDepartment.GetValueOrDefault(dept.Id, 0m);
                deptTotals[dept.Id] += amount;
                AddCurrencyCell(PrimaryMatrixGrid, rowIndex, 2 + deptIndex, amount);
            }

            grandTotal += row.TotalAmount;
            AddCurrencyCell(PrimaryMatrixGrid, rowIndex, 2 + orderedDepts.Count, row.TotalAmount);
            rowIndex++;
        }

        PrimaryTotalGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        AddCell(PrimaryTotalGrid, 0, 0, "Total", HorizontalAlignment.Left);
        AddCell(PrimaryTotalGrid, 0, 1, string.Empty, HorizontalAlignment.Left);

        for (var deptIndex = 0; deptIndex < orderedDepts.Count; deptIndex++)
        {
            var total = deptTotals[orderedDepts[deptIndex].Id];
            AddCurrencyCell(PrimaryTotalGrid, 0, 2 + deptIndex, total);
        }

        AddCurrencyCell(PrimaryTotalGrid, 0, 2 + orderedDepts.Count, grandTotal);
    }

    private void BuildSecondaryMatrix(
        List<Department> productiveDepts,
        List<Department> serviceDepts,
        List<Department> orderedDepts,
        Dictionary<string, string> aliases)
    {
        ResetGrid(SecondaryMatrixGrid);
        ResetGrid(SecondaryTotalGrid);

        SetupDepartmentColumns(SecondaryMatrixGrid, productiveDepts, serviceDepts);
        SetupDepartmentColumns(SecondaryTotalGrid, productiveDepts, serviceDepts);

        AddDepartmentDoubleHeader(SecondaryMatrixGrid, "Concepto", "Base", productiveDepts, serviceDepts, aliases);

        var accumulated = orderedDepts.ToDictionary(d => d.Id, _ => 0m);
        foreach (var primaryRow in ViewModel.PrimaryRows)
        {
            foreach (var dept in orderedDepts)
            {
                accumulated[dept.Id] += primaryRow.AmountsByDepartment.GetValueOrDefault(dept.Id, 0m);
            }
        }

        var rowIndex = 2;
        var totalColumns = 2 + orderedDepts.Count + 1;

        foreach (var row in ViewModel.SecondaryRows)
        {
            SecondaryMatrixGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var sourceDept = orderedDepts.FirstOrDefault(d => d.Name == row.SourceDepartmentName);
            var rowLabel = sourceDept is not null && aliases.TryGetValue(sourceDept.Id, out var alias)
                ? $"{alias} · {row.SourceDepartmentName}"
                : row.SourceDepartmentName;

            if (sourceDept is not null)
            {
                var deptAlias = aliases.GetValueOrDefault(sourceDept.Id, "A");
                AddClickableRowOverlay(SecondaryMatrixGrid, rowIndex, totalColumns,
                    target => ShowDepartmentFlyout(target, sourceDept, deptAlias));
            }

            AddCell(SecondaryMatrixGrid, rowIndex, 0, rowLabel, HorizontalAlignment.Left);
            AddCell(SecondaryMatrixGrid, rowIndex, 1, row.AllocationBase, HorizontalAlignment.Left, isSecondary: true);

            for (var deptIndex = 0; deptIndex < orderedDepts.Count; deptIndex++)
            {
                var dept = orderedDepts[deptIndex];
                if (sourceDept is not null && dept.Id == sourceDept.Id)
                {
                    accumulated[dept.Id] = 0m;
                    AddCurrencyCell(SecondaryMatrixGrid, rowIndex, 2 + deptIndex, -row.TotalDistributed);
                }
                else
                {
                    var received = row.AmountsByDepartment.GetValueOrDefault(dept.Id, 0m);
                    accumulated[dept.Id] += received;
                    AddCurrencyCell(SecondaryMatrixGrid, rowIndex, 2 + deptIndex, received);
                }
            }

            AddCurrencyCell(SecondaryMatrixGrid, rowIndex, 2 + orderedDepts.Count, 0m);
            rowIndex++;
        }

        SecondaryTotalGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        AddCell(SecondaryTotalGrid, 0, 0, "Total", HorizontalAlignment.Left);
        AddCell(SecondaryTotalGrid, 0, 1, string.Empty, HorizontalAlignment.Left);

        var finalGrandTotal = 0m;
        for (var deptIndex = 0; deptIndex < orderedDepts.Count; deptIndex++)
        {
            var dept = orderedDepts[deptIndex];
            var value = dept.Type == DepartmentType.Service ? 0m : accumulated[dept.Id];
            finalGrandTotal += value;
            AddCurrencyCell(SecondaryTotalGrid, 0, 2 + deptIndex, value);
        }

        AddCurrencyCell(SecondaryTotalGrid, 0, 2 + orderedDepts.Count, finalGrandTotal);
    }

    private void BuildFinalMatrix(
        List<Department> productiveDepts,
        List<Department> orderedDepts,
        Dictionary<string, string> aliases)
    {
        ResetGrid(FinalMatrixGrid);
        ResetGrid(FinalTotalGrid);

        var orders = _stateService.CurrentProject.ProductionOrders;

        SetupFinalColumns(FinalMatrixGrid, orders);
        SetupFinalColumns(FinalTotalGrid, orders);

        FinalMatrixGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        FinalMatrixGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        AddHeaderCell(FinalMatrixGrid, 0, 0, "Departamento", HorizontalAlignment.Left, rowSpan: 2);
        AddHeaderCell(FinalMatrixGrid, 0, 1, "Base", HorizontalAlignment.Left, rowSpan: 2);
        if (orders.Count > 0)
        {
            AddHeaderCell(FinalMatrixGrid, 0, 2, "Órdenes de producción", HorizontalAlignment.Right,
                columnSpan: orders.Count);
        }

        AddHeaderCell(FinalMatrixGrid, 0, 2 + orders.Count, "Total ($)", HorizontalAlignment.Right, rowSpan: 2);

        for (var orderIndex = 0; orderIndex < orders.Count; orderIndex++)
        {
            AddHeaderCell(FinalMatrixGrid, 1, 2 + orderIndex, orders[orderIndex].Code, HorizontalAlignment.Right);
        }

        var finalDeptCosts = orderedDepts.ToDictionary(d => d.Id, _ => 0m);
        foreach (var row in ViewModel.PrimaryRows)
        {
            foreach (var dept in orderedDepts)
            {
                finalDeptCosts[dept.Id] += row.AmountsByDepartment.GetValueOrDefault(dept.Id, 0m);
            }
        }

        foreach (var row in ViewModel.SecondaryRows)
        {
            foreach (var dept in productiveDepts)
            {
                finalDeptCosts[dept.Id] += row.AmountsByDepartment.GetValueOrDefault(dept.Id, 0m);
            }
        }

        var rowIndex = 2;
        var totalColumns = 2 + orders.Count + 1;
        var orderTotals = orders.ToDictionary(o => o.Id, _ => 0m);
        var grandTotal = 0m;

        foreach (var dept in productiveDepts)
        {
            FinalMatrixGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var deptAlias = aliases.GetValueOrDefault(dept.Id, "A");
            AddClickableRowOverlay(FinalMatrixGrid, rowIndex, totalColumns,
                target => ShowDepartmentFlyout(target, dept, deptAlias));

            var deptCost = finalDeptCosts.GetValueOrDefault(dept.Id, 0m);
            var totalDeptHours = orders.Sum(o => o.DirectLaborEntries
                .Where(e => e.DepartmentId == dept.Id)
                .Sum(e => e.Hours));

            AddCell(FinalMatrixGrid, rowIndex, 0, $"{deptAlias} · {dept.Name}", HorizontalAlignment.Left);
            AddCell(FinalMatrixGrid, rowIndex, 1, "Horas MOD", HorizontalAlignment.Left, isSecondary: true);

            for (var orderIndex = 0; orderIndex < orders.Count; orderIndex++)
            {
                var order = orders[orderIndex];
                var orderHours = order.DirectLaborEntries
                    .Where(e => e.DepartmentId == dept.Id)
                    .Sum(e => e.Hours);
                var share = totalDeptHours > 0m ? deptCost * (orderHours / totalDeptHours) : 0m;
                orderTotals[order.Id] += share;
                AddCurrencyCell(FinalMatrixGrid, rowIndex, 2 + orderIndex, share);
            }

            grandTotal += deptCost;
            AddCurrencyCell(FinalMatrixGrid, rowIndex, 2 + orders.Count, deptCost);
            rowIndex++;
        }

        FinalTotalGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        AddCell(FinalTotalGrid, 0, 0, "Total", HorizontalAlignment.Left);
        AddCell(FinalTotalGrid, 0, 1, string.Empty, HorizontalAlignment.Left);

        for (var orderIndex = 0; orderIndex < orders.Count; orderIndex++)
        {
            var orderTotal = orderTotals[orders[orderIndex].Id];
            AddCurrencyCell(FinalTotalGrid, 0, 2 + orderIndex, orderTotal);
        }

        AddCurrencyCell(FinalTotalGrid, 0, 2 + orders.Count, grandTotal);
    }

    private void BuildCupMatrix()
    {
        ResetGrid(CupMatrixGrid);
        ResetGrid(CupTotalProcessedGrid);
        ResetGrid(CupFinishedUnitsGrid);
        ResetGrid(CupUnitCostGrid);
        ResetGrid(CupSalePriceGrid);
        ResetGrid(CupIfppGrid);

        var summaries = OrdersViewModel.OrderSummaries.ToList();

        SetupCupColumns(CupMatrixGrid, summaries);
        SetupCupColumns(CupTotalProcessedGrid, summaries);
        SetupCupColumns(CupFinishedUnitsGrid, summaries);
        SetupCupColumns(CupUnitCostGrid, summaries);
        SetupCupColumns(CupSalePriceGrid, summaries);
        SetupCupColumns(CupIfppGrid, summaries);

        CupMatrixGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        CupMatrixGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        AddHeaderCell(CupMatrixGrid, 0, 0, "Concepto", HorizontalAlignment.Left, rowSpan: 2);
        if (summaries.Count > 0)
        {
            AddHeaderCell(CupMatrixGrid, 0, 1, "Órdenes de producción", HorizontalAlignment.Right,
                columnSpan: summaries.Count);
        }

        AddHeaderCell(CupMatrixGrid, 0, 1 + summaries.Count, "Total ($)", HorizontalAlignment.Right, rowSpan: 2);

        for (var index = 0; index < summaries.Count; index++)
        {
            var summary = summaries[index];
            var order = _stateService.CurrentProject.ProductionOrders
                .FirstOrDefault(o => o.Id == summary.ProductionOrderId);

            var headerButton = new Button
            {
                Content = order?.Code ?? summary.OrderCode,
                Style = (Style)Application.Current.Resources["SubtleButtonStyle"],
                HorizontalAlignment = HorizontalAlignment.Right,
                Padding = new Thickness(6, 2, 6, 2),
                FontSize = 12
            };

            if (order is not null)
            {
                headerButton.Click += (sender, _) =>
                {
                    if (sender is FrameworkElement target)
                    {
                        ShowOrderFlyout(target, order);
                    }
                };
            }

            Grid.SetRow(headerButton, 1);
            Grid.SetColumn(headerButton, 1 + index);
            CupMatrixGrid.Children.Add(headerButton);
        }

        var rowIndex = 2;
        AddCupCurrencyRow(CupMatrixGrid, rowIndex++, "Inv. inicial de PP", summaries, s => s.InitialWorkInProcessCost,
            summaries.Sum(s => s.InitialWorkInProcessCost));
        AddCupCurrencyRow(CupMatrixGrid, rowIndex++, "MP directa", summaries, s => s.DirectMaterialCost,
            summaries.Sum(s => s.DirectMaterialCost));
        AddCupCurrencyRow(CupMatrixGrid, rowIndex++, "MOD", summaries, s => s.DirectLaborCost,
            summaries.Sum(s => s.DirectLaborCost));
        AddCupCurrencyRow(CupMatrixGrid, rowIndex, "CI (GIF)", summaries, s => s.AppliedOverheadCost,
            summaries.Sum(s => s.AppliedOverheadCost));

        AddCupCurrencyRow(CupTotalProcessedGrid, 0, "Total procesado", summaries, s => s.TotalProductionCost,
            summaries.Sum(s => s.TotalProductionCost));

        AddCupTextRow(
            CupFinishedUnitsGrid,
            0,
            "Unidades terminadas",
            summaries,
            s => s.Status == OrderStatus.Finished && s.ProducedUnits > 0m ? $"{s.ProducedUnits:N0}" : "—",
            FormatUnits(summaries.Where(s => s.Status == OrderStatus.Finished).Sum(s => s.ProducedUnits)));

        AddCupTextRow(
            CupUnitCostGrid,
            0,
            "Costo unitario de producción",
            summaries,
            s => s.Status == OrderStatus.Finished && s.UnitProductionCost > 0m ? $"{s.UnitProductionCost:C4}" : "—",
            "—");

        AddCupTextRow(
            CupSalePriceGrid,
            0,
            "Precio de venta",
            summaries,
            s => s.Status == OrderStatus.Finished && s.UnitSalePrice > 0m ? $"{s.UnitSalePrice:C4}" : "—",
            "—");

        AddCupCurrencyRow(
            CupIfppGrid,
            0,
            "Valor de IFPP",
            summaries,
            s => s.Status == OrderStatus.InProcess ? s.TotalProductionCost : 0m,
            summaries.Where(s => s.Status == OrderStatus.InProcess).Sum(s => s.TotalProductionCost));
    }

    private static void AddCupCurrencyRow(
        Grid targetGrid,
        int rowIndex,
        string label,
        List<OrderCostSummary> summaries,
        Func<OrderCostSummary, decimal> selector,
        decimal totalValue)
    {
        targetGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        AddCell(targetGrid, rowIndex, 0, label, HorizontalAlignment.Left);

        for (var index = 0; index < summaries.Count; index++)
        {
            AddCurrencyCell(targetGrid, rowIndex, 1 + index, selector(summaries[index]));
        }

        AddCurrencyCell(targetGrid, rowIndex, 1 + summaries.Count, totalValue);
    }

    private static void AddCupTextRow(
        Grid targetGrid,
        int rowIndex,
        string label,
        List<OrderCostSummary> summaries,
        Func<OrderCostSummary, string> selector,
        string totalDisplay)
    {
        targetGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        AddCell(targetGrid, rowIndex, 0, label, HorizontalAlignment.Left);

        for (var index = 0; index < summaries.Count; index++)
        {
            AddCell(targetGrid, rowIndex, 1 + index, selector(summaries[index]), HorizontalAlignment.Right);
        }

        AddCell(targetGrid, rowIndex, 1 + summaries.Count, totalDisplay, HorizontalAlignment.Right);
    }

    private static string FormatUnits(decimal value) => value == 0m ? "—" : $"{value:N0}";

    private static void ResetGrid(Grid targetGrid)
    {
        targetGrid.Children.Clear();
        targetGrid.RowDefinitions.Clear();
        targetGrid.ColumnDefinitions.Clear();
    }

    private static void SetupDepartmentColumns(Grid targetGrid, List<Department> productiveDepts,
        List<Department> serviceDepts)
    {
        targetGrid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star), MinWidth = 170 });
        targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

        foreach (var _ in productiveDepts)
        {
            targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        }

        foreach (var _ in serviceDepts)
        {
            targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        }

        targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });
    }

    private static void SetupFinalColumns(Grid targetGrid, List<ProductionOrder> orders)
    {
        targetGrid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star), MinWidth = 170 });
        targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

        foreach (var _ in orders)
        {
            targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(115) });
        }

        targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });
    }

    private static void SetupCupColumns(Grid targetGrid, List<OrderCostSummary> summaries)
    {
        targetGrid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = new GridLength(1, GridUnitType.Star), MinWidth = 210 });

        foreach (var _ in summaries)
        {
            targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });
        }

        targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
    }

    private static void AddDepartmentDoubleHeader(
        Grid targetGrid,
        string firstHeader,
        string secondHeader,
        List<Department> productiveDepts,
        List<Department> serviceDepts,
        Dictionary<string, string> aliases)
    {
        targetGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        targetGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        AddHeaderCell(targetGrid, 0, 0, firstHeader, HorizontalAlignment.Left, rowSpan: 2);
        AddHeaderCell(targetGrid, 0, 1, secondHeader, HorizontalAlignment.Left, rowSpan: 2);

        var columnOffset = 2;
        if (productiveDepts.Count > 0)
        {
            AddHeaderCell(targetGrid, 0, columnOffset, "Productivos ($)", HorizontalAlignment.Right,
                columnSpan: productiveDepts.Count);
            for (var index = 0; index < productiveDepts.Count; index++)
            {
                var dept = productiveDepts[index];
                var alias = aliases.GetValueOrDefault(dept.Id, "A");
                AddHeaderCell(targetGrid, 1, columnOffset + index, alias, HorizontalAlignment.Right,
                    tooltipText: dept.Name);
            }

            columnOffset += productiveDepts.Count;
        }

        if (serviceDepts.Count > 0)
        {
            AddHeaderCell(targetGrid, 0, columnOffset, "De servicio ($)", HorizontalAlignment.Right,
                columnSpan: serviceDepts.Count);
            for (var index = 0; index < serviceDepts.Count; index++)
            {
                var dept = serviceDepts[index];
                var alias = aliases.GetValueOrDefault(dept.Id, "A");
                AddHeaderCell(targetGrid, 1, columnOffset + index, alias, HorizontalAlignment.Right,
                    tooltipText: dept.Name);
            }

            columnOffset += serviceDepts.Count;
        }

        AddHeaderCell(targetGrid, 0, columnOffset, "Total ($)", HorizontalAlignment.Right, rowSpan: 2);
    }

    private static void AddHeaderCell(
        Grid targetGrid,
        int row,
        int column,
        string text,
        HorizontalAlignment alignment,
        int rowSpan = 1,
        int columnSpan = 1,
        string? tooltipText = null)
    {
        var block = new TextBlock
        {
            Text = text,
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            HorizontalAlignment = alignment,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Padding = new Thickness(0, 2, 0, 2)
        };

        if (!string.IsNullOrWhiteSpace(tooltipText))
        {
            ToolTipService.SetToolTip(block, tooltipText);
        }

        Grid.SetRow(block, row);
        Grid.SetColumn(block, column);
        if (rowSpan > 1)
        {
            Grid.SetRowSpan(block, rowSpan);
        }

        if (columnSpan > 1)
        {
            Grid.SetColumnSpan(block, columnSpan);
        }

        targetGrid.Children.Add(block);
    }

    private static void AddCurrencyCell(Grid targetGrid, int row, int column, decimal value)
    {
        if (value == 0m)
        {
            AddCell(targetGrid, row, column, "—", HorizontalAlignment.Right);
            return;
        }

        if (value < 0m)
        {
            AddCell(targetGrid, row, column, $"({Math.Abs(value):C2})", HorizontalAlignment.Right, isCritical: true);
            return;
        }

        AddCell(targetGrid, row, column, $"{value:C2}", HorizontalAlignment.Right);
    }

    private static void AddCell(
        Grid targetGrid,
        int row,
        int column,
        string text,
        HorizontalAlignment alignment,
        bool isSecondary = false,
        bool isCritical = false)
    {
        var block = new TextBlock
        {
            Text = text,
            HorizontalAlignment = alignment,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Padding = new Thickness(0, 6, 0, 6),
            IsHitTestVisible = false
        };

        if (isCritical)
        {
            block.Foreground = GetCriticalBrush();
        }
        else if (isSecondary)
        {
            block.Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
        }

        Grid.SetRow(block, row);
        Grid.SetColumn(block, column);
        targetGrid.Children.Add(block);
    }

    private static Brush GetCriticalBrush()
    {
        if (Application.Current.Resources.TryGetValue("SystemFillColorCriticalBrush", out var resource)
            && resource is Brush criticalBrush)
        {
            return criticalBrush;
        }

        return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 196, 43, 28));
    }

    private static void AddClickableRowOverlay(Grid targetGrid, int row, int totalColumns,
        Action<FrameworkElement> onClick)
    {
        var hitArea = new Border
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            CornerRadius = new CornerRadius(4)
        };

        hitArea.PointerEntered += (_, _) =>
            hitArea.Background = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];
        hitArea.PointerExited += (_, _) => hitArea.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        hitArea.Tapped += (sender, _) =>
        {
            if (sender is FrameworkElement element)
            {
                onClick(element);
            }
        };

        Grid.SetRow(hitArea, row);
        Grid.SetColumn(hitArea, 0);
        Grid.SetColumnSpan(hitArea, totalColumns);
        targetGrid.Children.Add(hitArea);
    }

    private void ShowExpenseFlyout(FrameworkElement target, OverheadExpense expense)
    {
        var flyout = new MenuFlyout();

        var editItem = new MenuFlyoutItem
        {
            Text = "Editar gasto",
            Icon = new FontIcon { Glyph = "\uE70F" }
        };
        editItem.Click += async (_, _) =>
        {
            var dialog = new OverheadExpenseDialog(_stateService.CurrentProject.Departments, expense);
            await _dialogService.ShowDialogAsync(dialog);

            if (dialog.ViewModel.IsOperationSuccessful && dialog.ViewModel.ResultExpense is not null)
            {
                ViewModel.UpdateExpense(dialog.ViewModel.ResultExpense);
            }
        };
        flyout.Items.Add(editItem);

        var deleteItem = new MenuFlyoutItem
        {
            Text = "Eliminar gasto",
            Icon = new FontIcon { Glyph = "\uE74D" }
        };
        deleteItem.Click += async (_, _) => await ViewModel.DeleteExpenseAsync(expense);
        flyout.Items.Add(deleteItem);

        flyout.ShowAt(target);
    }

    private void ShowDepartmentFlyout(FrameworkElement target, Department dept, string deptAlias)
    {
        var flyout = new MenuFlyout();

        var editItem = new MenuFlyoutItem
        {
            Text = "Editar departamento",
            Icon = new FontIcon { Glyph = "\uE70F" }
        };
        editItem.Click += async (_, _) =>
        {
            var dialog = new DepartmentDialog(deptAlias, dept);
            await _dialogService.ShowDialogAsync(dialog);

            if (dialog.ViewModel.IsOperationSuccessful && dialog.ViewModel.ResultDepartment is not null)
            {
                ViewModel.UpdateDepartment(dialog.ViewModel.ResultDepartment);
            }
        };
        flyout.Items.Add(editItem);

        var deleteItem = new MenuFlyoutItem
        {
            Text = "Eliminar departamento",
            Icon = new FontIcon { Glyph = "\uE74D" }
        };
        deleteItem.Click += async (_, _) => await ViewModel.DeleteDepartmentAsync(dept);
        flyout.Items.Add(deleteItem);

        flyout.ShowAt(target);
    }

    private void ShowOrderFlyout(FrameworkElement target, ProductionOrder order)
    {
        var flyout = new MenuFlyout();

        var editItem = new MenuFlyoutItem
        {
            Text = "Editar orden",
            Icon = new FontIcon { Glyph = "\uE70F" }
        };
        editItem.Click += async (_, _) =>
        {
            var productive = _stateService.CurrentProject.Departments
                .Where(d => d.Type == DepartmentType.Productive)
                .ToList();

            var dialog = new ProductionOrderDialog(productive, order.Code, order);
            await _dialogService.ShowDialogAsync(dialog);

            if (dialog.ViewModel.IsOperationSuccessful && dialog.ViewModel.ResultOrder is not null)
            {
                OrdersViewModel.UpdateOrder(dialog.ViewModel.ResultOrder);
            }
        };
        flyout.Items.Add(editItem);

        var deleteItem = new MenuFlyoutItem
        {
            Text = "Eliminar orden",
            Icon = new FontIcon { Glyph = "\uE74D" }
        };
        deleteItem.Click += async (_, _) => await OrdersViewModel.DeleteOrderAsync(order);
        flyout.Items.Add(deleteItem);

        flyout.ShowAt(target);
    }

    private async void OnAddExpenseClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OverheadExpenseDialog(_stateService.CurrentProject.Departments);
        await _dialogService.ShowDialogAsync(dialog);

        if (dialog.ViewModel.IsOperationSuccessful && dialog.ViewModel.ResultExpense is not null)
        {
            ViewModel.AddExpense(dialog.ViewModel.ResultExpense);
        }
    }

    private async void OnAddDepartmentClick(object sender, RoutedEventArgs e)
    {
        var nextAlias = GetDepartmentAlias(_stateService.CurrentProject.Departments.Count);
        var dialog = new DepartmentDialog(nextAlias);
        await _dialogService.ShowDialogAsync(dialog);

        if (dialog.ViewModel.IsOperationSuccessful && dialog.ViewModel.ResultDepartment is not null)
        {
            ViewModel.AddDepartment(dialog.ViewModel.ResultDepartment);
        }
    }

    private async void OnAddOrderClick(object sender, RoutedEventArgs e)
    {
        var productive = _stateService.CurrentProject.Departments
            .Where(d => d.Type == DepartmentType.Productive)
            .ToList();

        var nextCode = OrdersViewModel.GetNextOrderCode();
        var dialog = new ProductionOrderDialog(productive, nextCode);
        await _dialogService.ShowDialogAsync(dialog);

        if (dialog.ViewModel.IsOperationSuccessful && dialog.ViewModel.ResultOrder is not null)
        {
            OrdersViewModel.AddOrder(dialog.ViewModel.ResultOrder);
        }
    }

    private MenuFlyout BuildFilterFlyout()
    {
        var flyout = new MenuFlyout();
        flyout.Items.Add(CreateFilterItem("Todo", null));
        flyout.Items.Add(CreateFilterItem("Solo prorrateos", "Prorations"));
        flyout.Items.Add(CreateFilterItem("Solo costo unitario de producción", "Cup"));
        return flyout;
    }

    private ToggleMenuFlyoutItem CreateFilterItem(string text, string? filter)
    {
        var item = new ToggleMenuFlyoutItem
        {
            Text = text,
            IsChecked = _sectionFilter == filter
        };

        item.Click += (_, _) =>
        {
            _sectionFilter = filter;
            Bindings.Update();
            NotifyHeaderChanged();
        };

        return item;
    }

    private void NotifyHeaderChanged() =>
        HeaderStateChanged?.Invoke(this, EventArgs.Empty);
}