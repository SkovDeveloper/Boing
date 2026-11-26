using Microsoft.UI.Xaml.Navigation;

namespace Boing.Views;

public sealed partial class MainPage
{
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly Dictionary<string, Type> _routes = new();
    private IDynamicCommandPage? _activeDynamicPage;

    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        InitializeComponent();

        ViewModel = AppServices.GetService<MainViewModel>();
        _navigationService = AppServices.GetService<INavigationService>();
        _dialogService = AppServices.GetService<IDialogService>();

        RegisterRoute("Warehouse", typeof(WarehousePage));
        RegisterRoute("Proration", typeof(ProrationPage));
        RegisterRoute("Ecpv", typeof(EcpvPage));

        Loaded += OnMainPageLoaded;
    }

    private void RegisterRoute(string key, Type pageType)
    {
        _routes[key] = pageType;
    }

    private async void OnMainPageLoaded(object sender, RoutedEventArgs args)
    {
        Loaded -= OnMainPageLoaded;

        _navigationService.Initialize(MainPageFrame);
        _dialogService.Initialize(() => XamlRoot);

        await ViewModel.InitializeAsync();

        if (_routes.TryGetValue("Warehouse", out Type? defaultPage))
        {
            _navigationService.NavigateTo(defaultPage);
        }
    }

    private void OnStaticNavigationClick(object sender, RoutedEventArgs args)
    {
        if (sender is not FrameworkElement element || element.Tag is not string routeKey)
        {
            return;
        }

        if (_routes.TryGetValue(routeKey, out Type? targetPage) && MainPageFrame.CurrentSourcePageType != targetPage)
        {
            _navigationService.NavigateTo(targetPage);
        }
    }

    private void OnMainPageFrameNavigated(object sender, NavigationEventArgs args)
    {
        if (_activeDynamicPage is not null)
        {
            _activeDynamicPage.HeaderStateChanged -= OnActivePageHeaderStateChanged;
            _activeDynamicPage = null;
        }

        if (args.Content is IDynamicCommandPage dynamicPage)
        {
            _activeDynamicPage = dynamicPage;
            _activeDynamicPage.HeaderStateChanged += OnActivePageHeaderStateChanged;
        }

        MainScrollViewer.ChangeView(null, 0, null, true);
        RefreshDynamicHeader();
    }

    private void OnActivePageHeaderStateChanged(object? sender, EventArgs args)
    {
        RefreshDynamicHeader();
    }

    private async void OnCreditsClick(object sender, RoutedEventArgs args)
    {
        CreditsDialog dialog = new();
        await _dialogService.ShowDialogAsync(dialog);
    }

    private void RefreshDynamicHeader()
    {
        DynamicCommandBar.PrimaryCommands.Clear();

        if (_activeDynamicPage is null)
        {
            return;
        }

        HeaderTitle.Text = _activeDynamicPage.PageTitle;
        HeaderSubtitle.Text = _activeDynamicPage.PageSubtitle;

        foreach (ICommandBarElement element in _activeDynamicPage.GetDynamicCommands())
        {
            DynamicCommandBar.PrimaryCommands.Add(element);
        }
    }
}