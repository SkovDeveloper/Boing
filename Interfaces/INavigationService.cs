using System.ComponentModel;

namespace Boing.Interfaces;

public interface INavigationService : INotifyPropertyChanged
{
    bool CanGoBack { get; }
    void Initialize(Frame frame);
    bool NavigateTo(Type pageType, object? parameter = null);
    bool GoBack();
}