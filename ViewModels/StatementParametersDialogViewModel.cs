namespace Boing.ViewModels;

public sealed partial class StatementParametersDialogViewModel : DialogViewModel
{
    public string DialogTitle => "Parámetros de estados financieros";

    public string DialogCaption =>
        "Ajusta el inventario inicial de artículos terminados y los gastos de operación del periodo.";

    public string SubmitButtonText => "Guardar";

    [ObservableProperty] public partial double InitialFinishedGoods { get; set; }

    [ObservableProperty] public partial double SellingExpenses { get; set; }

    [ObservableProperty] public partial double AdminExpenses { get; set; }

    public StatementParametersDialogViewModel(decimal currentInitialFg, decimal currentSelling, decimal currentAdmin)
    {
        InitialFinishedGoods = (double)currentInitialFg;
        SellingExpenses = (double)currentSelling;
        AdminExpenses = (double)currentAdmin;
    }

    protected override Task ExecuteSubmitActionAsync()
    {
        if (double.IsNaN(InitialFinishedGoods) || InitialFinishedGoods < 0)
        {
            ShowError("Inventario inválido", "El inventario inicial debe ser mayor o igual a cero.");
            return Task.CompletedTask;
        }

        if (double.IsNaN(SellingExpenses) || SellingExpenses < 0)
        {
            ShowError("Gastos de venta inválidos", "Los gastos de venta deben ser mayores o iguales a cero.");
            return Task.CompletedTask;
        }

        if (double.IsNaN(AdminExpenses) || AdminExpenses < 0)
        {
            ShowError("Gastos de administración inválidos",
                "Los gastos de administración deben ser mayores o iguales a cero.");
            return Task.CompletedTask;
        }

        IsOperationSuccessful = true;
        CloseDialogAction?.Invoke();
        return Task.CompletedTask;
    }
}