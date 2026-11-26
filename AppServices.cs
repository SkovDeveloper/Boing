using Boing.Interfaces;
using Boing.Services;
using Boing.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Boing;

public static class AppServices
{
    private static IServiceProvider? _Provider = null;

    public static void Configure()
    {
        ServiceCollection Services = new();

        Services.AddSingleton<IWarehouseEngine, WarehouseEngine>();
        Services.AddSingleton<IProrationEngine, ProrationEngine>();
        Services.AddSingleton<ICostEngine, CostEngine>();
        Services.AddSingleton<IProjectStorageService, ProjectStorageService>();
        Services.AddSingleton<IProjectStateService, ProjectStateService>();
        Services.AddSingleton<IOrderCodeGeneratorService, OrderCodeGeneratorService>();
        Services.AddSingleton<INavigationService, NavigationService>();
        Services.AddSingleton<IDialogService, DialogService>();
        Services.AddSingleton<IFilePickerService, FilePickerService>();
        Services.AddSingleton<StatementsViewModel>();

        Services.AddSingleton<MainViewModel>();
        Services.AddSingleton<OverviewViewModel>();
        Services.AddSingleton<WarehouseViewModel>();
        Services.AddSingleton<OrdersViewModel>();
        Services.AddSingleton<ProrationViewModel>();
        Services.AddSingleton<StatementsViewModel>();

        _Provider = Services.BuildServiceProvider();
    }

    public static T GetService<T>() where T : class
    {
        if (_Provider is null)
        {
            throw new InvalidOperationException("AppServices has not been configured.");
        }

        return _Provider.GetRequiredService<T>();
    }
}