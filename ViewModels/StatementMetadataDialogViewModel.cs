namespace Boing.ViewModels;

public sealed partial class StatementMetadataDialogViewModel : DialogViewModel
{
    public string DialogTitle => "Encabezado y firmas";

    public string DialogCaption =>
        "Configura el nombre de la empresa, el periodo contable y las firmas de los estados financieros.";

    public string SubmitButtonText => "Guardar";

    [ObservableProperty] public partial string CompanyName { get; set; } = string.Empty;

    [ObservableProperty] public partial DateTimeOffset? PeriodStart { get; set; }

    [ObservableProperty] public partial DateTimeOffset? PeriodEnd { get; set; }

    [ObservableProperty] public partial string PreparedBy { get; set; } = string.Empty;

    [ObservableProperty] public partial string AuthorizedBy { get; set; } = string.Empty;

    public StatementMetadataDialogViewModel(
        string currentCompanyName,
        DateOnly currentStart,
        DateOnly currentEnd,
        string currentPreparedBy,
        string currentAuthorizedBy)
    {
        CompanyName = currentCompanyName;
        PeriodStart = new DateTimeOffset(currentStart.ToDateTime(TimeOnly.MinValue));
        PeriodEnd = new DateTimeOffset(currentEnd.ToDateTime(TimeOnly.MinValue));
        PreparedBy = currentPreparedBy;
        AuthorizedBy = currentAuthorizedBy;
    }

    protected override Task ExecuteSubmitActionAsync()
    {
        if (string.IsNullOrWhiteSpace(CompanyName))
        {
            ShowError("Empresa requerida", "Ingresa el nombre de la empresa.");
            return Task.CompletedTask;
        }

        if (PeriodStart is null || PeriodEnd is null)
        {
            ShowError("Periodo requerido", "Selecciona la fecha de inicio y de fin del periodo.");
            return Task.CompletedTask;
        }

        if (PeriodEnd.Value.Date < PeriodStart.Value.Date)
        {
            ShowError("Periodo inválido", "La fecha final no puede ser anterior a la fecha inicial.");
            return Task.CompletedTask;
        }

        IsOperationSuccessful = true;
        CloseDialogAction?.Invoke();
        return Task.CompletedTask;
    }
}