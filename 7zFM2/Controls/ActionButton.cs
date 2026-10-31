using System.Windows.Input;
using CommunityToolkit.Maui;

namespace SevenZip.FileManager2.Controls;

/// <summary>
/// Native button interaction with a separately sized image and optional caption.
/// Ordinary Image loading works across the GTK/AppKit backends where button-image
/// loading/layout is incomplete. Decorative views never intercept pointer input.
/// </summary>
public sealed partial class ActionButton : Grid
{
    [BindableProperty]
    public partial ImageSource? Source { get; set; }

    [BindableProperty]
    public partial string? Text { get; set; }

    [BindableProperty]
    public partial bool ShowText { get; set; }

    [BindableProperty]
    public partial ICommand? Command { get; set; }

    [BindableProperty]
    public partial object? CommandParameter { get; set; }

    public ActionButton()
    {
        var button = new Button { Padding = 0 };
        button.SetBinding(Button.CommandProperty, new Binding(nameof(Command), source: this));
        button.SetBinding(Button.CommandParameterProperty, new Binding(nameof(CommandParameter), source: this));
        button.SetBinding(SemanticProperties.DescriptionProperty, new Binding(nameof(Text), source: this));
        button.SetBinding(ToolTipProperties.TextProperty, new Binding(nameof(Text), source: this));

        var image = new Image
        {
            WidthRequest = 24,
            HeightRequest = 24,
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.Center,
            InputTransparent = true
        };
        image.SetBinding(Image.SourceProperty, new Binding(nameof(Source), source: this));

        var label = new Label
        {
            FontSize = 13,
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.NoWrap,
            InputTransparent = true
        };
        label.SetBinding(Label.TextProperty, new Binding(nameof(Text), source: this));
        label.SetBinding(IsVisibleProperty, new Binding(nameof(ShowText), source: this));
        label.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#202020"), Color.FromArgb("#EEEEEE"));

        var decoration = new VerticalStackLayout
        {
            Spacing = 4,
            Margin = 6,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true,
            Children = { image, label }
        };
        AutomationProperties.SetIsInAccessibleTree(decoration, false);
        AutomationProperties.SetIsInAccessibleTree(image, false);
        AutomationProperties.SetIsInAccessibleTree(label, false);

        // Follow the actual Button state, including Command.CanExecute, rather than
        // only the containing view's explicit IsEnabled value.
        button.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(IsEnabled))
            {
                decoration.Opacity = button.IsEnabled ? 1 : 0.4;
            }
        };
        decoration.Opacity = button.IsEnabled ? 1 : 0.4;
        Children.Add(button);
        Children.Add(decoration);
    }
}
