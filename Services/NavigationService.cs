using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

namespace Boing.Services;

public sealed partial class NavigationService : ObservableObject, INavigationService
{
    private Frame? _frame;

    [ObservableProperty] public partial bool CanGoBack { get; private set; }

    public void Initialize(Frame frame)
    {
        if (_frame is not null)
        {
            _frame.Navigated -= OnFrameNavigated;
        }

        _frame = frame;
        _frame.Navigated += OnFrameNavigated;
        CanGoBack = _frame.CanGoBack;
    }

    public bool NavigateTo(Type pageType, object? parameter = null) =>
        _frame?.Navigate(pageType, parameter, new EntranceNavigationTransitionInfo()) ?? false;

    public bool GoBack()
    {
        if (_frame is null || !_frame.CanGoBack)
        {
            return false;
        }

        _frame.GoBack();
        return true;
    }

    private void OnFrameNavigated(object sender, NavigationEventArgs e) =>
        CanGoBack = _frame?.CanGoBack ?? false;
}