using Boing.Interfaces;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;

namespace Boing;

public sealed partial class MainWindow : Window
{
    public static MainWindow? Current { get; private set; }
    public INavigationService NavigationService { get; }

    public MainWindow()
    {
        Current = this;
        Activated += MainWindow_Activated;
        NavigationService = AppServices.GetService<INavigationService>();
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        SetTitleBar(WindowTitleBar);

        SetTaskbarIcon();

        if (AppWindow.Presenter is OverlappedPresenter Presenter)
        {
            Presenter.Maximize();
        }
    }

    public void NavigateToRoot(Type PageType, object? Parameter = null)
    {
        ContentFrame.Navigate(PageType, Parameter);
    }

    private void TitleBarBackRequested(TitleBar Sender, object Args)
    {
        if (NavigationService.CanGoBack)
        {
            NavigationService.GoBack();
            return;
        }

        if (ContentFrame.CanGoBack)
        {
            ContentFrame.GoBack();
        }
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        Activated -= MainWindow_Activated;
        SetTaskbarIcon();
        MaximizeWindow();
    }

    private void MaximizeWindow()
    {
        var windowId = Win32Interop
            .GetWindowIdFromWindow(WindowNative.GetWindowHandle(this));

        var appWindow = AppWindow.GetFromWindowId(windowId);

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }
    }

    private void SetTaskbarIcon()
    {
        if (AppWindow != null)
        {
            string iconPath = System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "Assets",
                "ApplicationImages",
                "TaskBarIcon.ico"
            );

            if (System.IO.File.Exists(iconPath))
            {
                AppWindow.SetIcon(iconPath);
            }
        }
    }
}