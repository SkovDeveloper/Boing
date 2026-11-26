using System.Collections.ObjectModel;

namespace Boing.ViewModels;

public sealed partial class ProrationViewModel : ObservableObject
{
    private readonly IProjectStateService _stateService;
    private readonly IDialogService _dialogService;

    public ObservableCollection<Department> Departments { get; } = [];
    public ObservableCollection<OverheadExpense> OverheadExpenses { get; } = [];
    public ObservableCollection<PrimaryProrationRow> PrimaryRows { get; } = [];
    public ObservableCollection<SecondaryProrationRow> SecondaryRows { get; } = [];
    public ObservableCollection<FinalProrationRow> FinalRows { get; } = [];

    [ObservableProperty] private decimal _totalOverheadApplied;

    public ProrationViewModel(IProjectStateService stateService, IDialogService dialogService)
    {
        _stateService = stateService;
        _dialogService = dialogService;
        _stateService.StateChanged += OnStateChanged;
        Refresh();
    }

    public void AddDepartment(Department newDepartment)
    {
        _stateService.CurrentProject.Departments.Add(newDepartment);
        _stateService.Recalculate();
    }

    public void UpdateDepartment(Department updatedDepartment)
    {
        var deptList = _stateService.CurrentProject.Departments;
        var index = deptList.FindIndex(d => d.Id == updatedDepartment.Id);
        if (index >= 0)
        {
            deptList[index] = updatedDepartment;
            _stateService.Recalculate();
        }
    }

    [RelayCommand]
    public async Task DeleteDepartmentAsync(Department? target)
    {
        if (target is null)
        {
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Eliminar departamento",
            $"¿Deseas eliminar el departamento '{target.Name}'?",
            "Eliminar",
            "Cancelar");

        if (!confirmed)
        {
            return;
        }

        _stateService.CurrentProject.Departments.RemoveAll(d => d.Id == target.Id);
        _stateService.Recalculate();
    }

    public void AddExpense(OverheadExpense newExpense)
    {
        _stateService.CurrentProject.OverheadExpenses.Add(newExpense);
        _stateService.Recalculate();
    }

    public void UpdateExpense(OverheadExpense updatedExpense)
    {
        var expenseList = _stateService.CurrentProject.OverheadExpenses;
        var index = expenseList.FindIndex(e => e.Id == updatedExpense.Id);
        if (index >= 0)
        {
            expenseList[index] = updatedExpense;
            _stateService.Recalculate();
        }
    }

    [RelayCommand]
    public async Task DeleteExpenseAsync(OverheadExpense? target)
    {
        if (target is null)
        {
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Eliminar gasto indirecto",
            $"¿Deseas eliminar el gasto '{target.Concept}'?",
            "Eliminar",
            "Cancelar");

        if (!confirmed)
        {
            return;
        }

        _stateService.CurrentProject.OverheadExpenses.RemoveAll(e => e.Id == target.Id);
        _stateService.Recalculate();
    }

    private void OnStateChanged(object? sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        Departments.Clear();
        foreach (var dept in _stateService.CurrentProject.Departments)
        {
            Departments.Add(dept);
        }

        OverheadExpenses.Clear();
        foreach (var expense in _stateService.CurrentProject.OverheadExpenses)
        {
            OverheadExpenses.Add(expense);
        }

        var proration = _stateService.CurrentResult.Proration;

        PrimaryRows.Clear();
        foreach (var row in proration.PrimaryRows)
        {
            PrimaryRows.Add(row);
        }

        SecondaryRows.Clear();
        foreach (var row in proration.SecondaryRows)
        {
            SecondaryRows.Add(row);
        }

        FinalRows.Clear();
        foreach (var row in proration.FinalRows)
        {
            FinalRows.Add(row);
        }

        TotalOverheadApplied = proration.TotalOverheadApplied;
    }
}