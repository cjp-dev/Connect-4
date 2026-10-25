using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Connect4.App.Models;
using Connect4.App.ViewModels;

namespace Connect4.WPF;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public static readonly RoutedUICommand UserGuideCommand = new(
        "User guide", nameof(UserGuideCommand), typeof(MainWindow),
        new InputGestureCollection { new KeyGesture(Key.F1) });

    public static readonly RoutedUICommand DocumentationCommand = new(
        "Rules & Documentation", nameof(DocumentationCommand), typeof(MainWindow),
        new InputGestureCollection { new KeyGesture(Key.F1, ModifierKeys.Shift) });

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        CommandBindings.Add(new CommandBinding(UserGuideCommand, OnUserGuideExecuted));
        CommandBindings.Add(new CommandBinding(DocumentationCommand, OnDocumentationExecuted));

        Restore(viewModel.WindowPlacement);
    }

    private void OnUserGuideExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        OpenUrl("https://github.com/cjp-dev/Connect-4/blob/main/docs/brain/13-gui-and-settings.md");
    }

    private void OnDocumentationExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        OpenUrl("https://github.com/cjp-dev/Connect-4/blob/main/docs/brain/README.md");
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _viewModel.Stop();
        Rect bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        _viewModel.SaveWindowPlacement(new WindowPlacement(bounds.Left, bounds.Top, bounds.Width, bounds.Height, WindowState == WindowState.Maximized));
        base.OnClosing(e);
    }

    // Only restore a position that is still on a screen (a monitor may have been removed).
    private void Restore(WindowPlacement? placement)
    {
        if (placement is null)
        {
            return;
        }

        var screen = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        var window = new Rect(placement.Left, placement.Top, Math.Max(placement.Width, MinWidth), Math.Max(placement.Height, MinHeight));
        if (!screen.IntersectsWith(window) || !screen.Contains(new Point(window.Left + 50, window.Top + 20)))
        {
            return;
        }

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = window.Left;
        Top = window.Top;
        Width = window.Width;
        Height = window.Height;
        if (placement.Maximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void OnExit(object sender, RoutedEventArgs e) => Close();
}
