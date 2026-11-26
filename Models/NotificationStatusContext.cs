using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace Boing.Models;

public partial class NotificationStatusContext : ObservableObject
{
    [ObservableProperty] public partial bool IsOpen { get; set; } = false;
    [ObservableProperty] public partial string Title { get; set; } = string.Empty;
    [ObservableProperty] public partial string Message { get; set; } = string.Empty;
    [ObservableProperty] public partial InfoBarSeverity Severity { get; set; } = InfoBarSeverity.Informational;
}