using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Themes.Fluent;
using Aegis.Payroll.Application;

namespace Aegis.Payroll.Desktop;
internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => AppBuilder.Configure<PayrollApp>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
}
public sealed class PayrollApp : Avalonia.Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var panel = new StackPanel { Margin = new Thickness(24), Spacing = 12 };
            panel.Children.Add(new TextBlock { Text = "Aegis Payroll", FontSize = 28 });
            panel.Children.Add(new TextBlock { Text = "Foundation shell — payroll processing unavailable", FontSize = 18 });
            panel.Children.Add(new TextBlock { Text = "Desktop authentication and operational API workflows await implementation.", TextWrapping = Avalonia.Media.TextWrapping.Wrap });
            foreach (var item in ShellCatalog.Capabilities)
                panel.Children.Add(new TextBlock { Text = $"{item.Name} · Implementation pending" });
            desktop.MainWindow = new Window { Title = "Aegis Payroll", Width = 1050, Height = 760,
                Content = new ScrollViewer { Content = panel, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled } };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
