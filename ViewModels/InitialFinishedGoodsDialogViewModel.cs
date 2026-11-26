namespace Boing.ViewModels;

public sealed partial class InitialFinishedGoodsDialogViewModel : DialogViewModel
{
    public string DialogTitle => "Inventario inicial de artículos terminados";

    public string DialogCaption =>
        "Define el saldo inicial de artículos terminados del periodo para el cálculo del costo de lo vendido.";

    public string SubmitButtonText => "Guardar";

    public decimal ResultAmount { get; private set; }

    [ObservableProperty] public partial double Amount { get; set; }

    public InitialFinishedGoodsDialogViewModel(decimal currentAmount)
    {
        Amount = (double)currentAmount;
    }

    protected override Task ExecuteSubmitActionAsync()
    {
        if (double.IsNaN(Amount) || Amount < 0)
        {
            ShowError("Monto inválido", "El inventario inicial debe ser mayor o igual a cero.");
            return Task.CompletedTask;
        }

        ResultAmount = (decimal)Amount;
        IsOperationSuccessful = true;
        CloseDialogAction?.Invoke();
        return Task.CompletedTask;
    }
}