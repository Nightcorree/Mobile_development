using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ApiTester.Gui.Views;

public partial class ConfirmDialog : UserControl
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
