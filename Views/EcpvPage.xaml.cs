using Boing.Dialogs;
using Boing.Interfaces;
using Boing.Models;
using Boing.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Boing.Views;

public sealed partial class EcpvPage : Page, IDynamicCommandPage
{
    private readonly IDialogService _dialogService;
    private Polyline? _activeStroke;
    private Canvas? _activeCanvas;

    public StatementsViewModel ViewModel { get; }

    public string PageTitle => ViewModel.PageTitle;
    public string PageSubtitle => ViewModel.PageSubtitle;
    public string PageIconGlyph => "\uE719";

    public event EventHandler? HeaderStateChanged;

    public EcpvPage()
    {
        ViewModel = AppServices.GetService<StatementsViewModel>();
        _dialogService = AppServices.GetService<IDialogService>();
        InitializeComponent();

        ViewModel.HeaderChanged += OnViewModelHeaderChanged;
        Unloaded += (_, _) => ViewModel.HeaderChanged -= OnViewModelHeaderChanged;

        RenderSavedSignatures();
    }

    public IReadOnlyList<ICommandBarElement> GetDynamicCommands()
    {
        List<ICommandBarElement> commands = [];

        var metadataButton = new AppBarButton
        {
            Label = "Encabezado",
            Icon = new FontIcon { Glyph = "\uE787" }
        };
        metadataButton.Click += OnEditMetadataClick;
        commands.Add(metadataButton);

        var parametersButton = new AppBarButton
        {
            Label = "Parámetros",
            Icon = new FontIcon { Glyph = "\uE713" }
        };
        parametersButton.Click += OnEditParametersClick;
        commands.Add(parametersButton);

        var filterButton = new AppBarButton
        {
            Label = "Filtrar",
            Icon = new FontIcon { Glyph = "\uE71C" },
            Flyout = BuildFilterFlyout()
        };
        commands.Add(filterButton);

        return commands;
    }

    private void OnViewModelHeaderChanged(object? sender, EventArgs e) =>
        HeaderStateChanged?.Invoke(this, EventArgs.Empty);

    private void RenderSavedSignatures()
    {
        RenderStrokes(PreparedSignatureCanvas, ViewModel.Settings.PreparedSignatureStrokes);
        RenderStrokes(AuthorizedSignatureCanvas, ViewModel.Settings.AuthorizedSignatureStrokes);
    }

    private static void RenderStrokes(Canvas canvas, List<List<SignaturePoint>> strokes)
    {
        canvas.Children.Clear();
        foreach (var strokePoints in strokes)
        {
            var polyline = CreateStrokePolyline();
            foreach (var point in strokePoints)
            {
                polyline.Points.Add(new Windows.Foundation.Point(point.X, point.Y));
            }

            canvas.Children.Add(polyline);
        }
    }

    private static Polyline CreateStrokePolyline() => new()
    {
        Stroke = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"],
        StrokeThickness = 2,
        StrokeLineJoin = PenLineJoin.Round,
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round
    };

    private void OnSignaturePointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Canvas targetCanvas)
        {
            return;
        }

        _activeCanvas = targetCanvas;
        targetCanvas.CapturePointer(e.Pointer);

        _activeStroke = CreateStrokePolyline();
        var position = e.GetCurrentPoint(targetCanvas).Position;
        _activeStroke.Points.Add(position);
        targetCanvas.Children.Add(_activeStroke);
    }

    private void OnSignaturePointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_activeStroke is null || _activeCanvas is null || !ReferenceEquals(sender, _activeCanvas))
        {
            return;
        }

        var position = e.GetCurrentPoint(_activeCanvas).Position;
        if (position is { X: >= 0, Y: >= 0 } && position.X <= _activeCanvas.ActualWidth &&
            position.Y <= _activeCanvas.ActualHeight)
        {
            _activeStroke.Points.Add(position);
        }
    }

    private void OnSignaturePointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_activeCanvas is not null)
        {
            _activeCanvas.ReleasePointerCapture(e.Pointer);

            if (_activeStroke is not null && _activeStroke.Points.Count > 0)
            {
                var points = _activeStroke.Points
                    .Select(p => new SignaturePoint { X = p.X, Y = p.Y })
                    .ToList();

                if (ReferenceEquals(_activeCanvas, PreparedSignatureCanvas))
                {
                    ViewModel.AddPreparedSignatureStroke(points);
                }
                else if (ReferenceEquals(_activeCanvas, AuthorizedSignatureCanvas))
                {
                    ViewModel.AddAuthorizedSignatureStroke(points);
                }
            }
        }

        _activeStroke = null;
        _activeCanvas = null;
    }

    private void OnClearPreparedSignatureClick(object sender, RoutedEventArgs e)
    {
        PreparedSignatureCanvas.Children.Clear();
        ViewModel.ClearPreparedSignature();
    }

    private void OnClearAuthorizedSignatureClick(object sender, RoutedEventArgs e)
    {
        AuthorizedSignatureCanvas.Children.Clear();
        ViewModel.ClearAuthorizedSignature();
    }

    private async void OnEditMetadataClick(object sender, RoutedEventArgs e)
    {
        var s = ViewModel.Settings;
        var dialog =
            new StatementMetadataDialog(s.CompanyName, s.PeriodStart, s.PeriodEnd, s.PreparedBy, s.AuthorizedBy);
        await _dialogService.ShowDialogAsync(dialog);

        if (dialog.ViewModel.IsOperationSuccessful)
        {
            ViewModel.UpdateMetadata(
                dialog.ViewModel.CompanyName,
                DateOnly.FromDateTime(dialog.ViewModel.PeriodStart!.Value.DateTime),
                DateOnly.FromDateTime(dialog.ViewModel.PeriodEnd!.Value.DateTime),
                dialog.ViewModel.PreparedBy,
                dialog.ViewModel.AuthorizedBy);
        }
    }

    private async void OnEditParametersClick(object sender, RoutedEventArgs e)
    {
        var s = ViewModel.Settings;
        var dialog = new StatementParametersDialog(s.InitialFinishedGoodsCost, s.SellingExpenses, s.AdminExpenses);
        await _dialogService.ShowDialogAsync(dialog);

        if (dialog.ViewModel.IsOperationSuccessful)
        {
            ViewModel.UpdateParameters(
                (decimal)dialog.ViewModel.InitialFinishedGoods,
                (decimal)dialog.ViewModel.SellingExpenses,
                (decimal)dialog.ViewModel.AdminExpenses);
        }
    }

    private MenuFlyout BuildFilterFlyout()
    {
        var flyout = new MenuFlyout();
        flyout.Items.Add(CreateFilterItem("Todo", null));
        flyout.Items.Add(CreateFilterItem("Solo ECPV", "Ecpv"));
        flyout.Items.Add(CreateFilterItem("Solo ER", "Er"));
        return flyout;
    }

    private ToggleMenuFlyoutItem CreateFilterItem(string text, string? filter)
    {
        var item = new ToggleMenuFlyoutItem
        {
            Text = text,
            IsChecked = ViewModel.SectionFilter == filter
        };

        item.Click += (_, _) => ViewModel.SetFilter(filter);
        return item;
    }
}