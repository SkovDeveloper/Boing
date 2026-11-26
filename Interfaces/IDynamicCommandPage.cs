namespace Boing.Interfaces;

public interface IDynamicCommandPage
{
    string PageTitle { get; }
    string PageSubtitle { get; }
    IReadOnlyList<ICommandBarElement> GetDynamicCommands();
    event EventHandler? HeaderStateChanged;
}