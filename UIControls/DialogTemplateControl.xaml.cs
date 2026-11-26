using Microsoft.UI.Xaml.Markup;

namespace Boing.UIControls;

[ContentProperty(Name = nameof(FormContent))]
public sealed partial class DialogTemplateControl
{
    public DialogTemplateControl()
    {
        InitializeComponent();
    }

    public DialogViewModel ViewModel
    {
        get => (DialogViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(DialogViewModel), typeof(DialogTemplateControl),
            new PropertyMetadata(null));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(DialogTemplateControl),
            new PropertyMetadata(string.Empty));

    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public static readonly DependencyProperty CaptionProperty =
        DependencyProperty.Register(nameof(Caption), typeof(string), typeof(DialogTemplateControl),
            new PropertyMetadata(string.Empty));

    public object FormContent
    {
        get => GetValue(FormContentProperty);
        set => SetValue(FormContentProperty, value);
    }

    public static readonly DependencyProperty FormContentProperty =
        DependencyProperty.Register(nameof(FormContent), typeof(object), typeof(DialogTemplateControl),
            new PropertyMetadata(null));

    public string SubmitButtonText
    {
        get => (string)GetValue(SubmitButtonTextProperty);
        set => SetValue(SubmitButtonTextProperty, value);
    }

    public static readonly DependencyProperty SubmitButtonTextProperty =
        DependencyProperty.Register(nameof(SubmitButtonText), typeof(string), typeof(DialogTemplateControl),
            new PropertyMetadata("Aceptar"));

    private Visibility GetVisibility(bool isProcessing) => isProcessing ? Visibility.Visible : Visibility.Collapsed;
    private bool Inverse(bool value) => !value;
    private double GetOpacity(bool isOpen) => isOpen ? 1.0 : 0.0;
}