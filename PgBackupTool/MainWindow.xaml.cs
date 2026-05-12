using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using PgBackupTool.ViewModels;

namespace PgBackupTool;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private const int GWL_STYLE = -16;
    private const int WS_MAXIMIZEBOX = 0x10000;

    public MainWindow()
    {
        _vm = new MainViewModel();
        DataContext = _vm;
        InitializeComponent();

        _vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.LogText))
                Dispatcher.InvokeAsync(() => LogScrollViewer.ScrollToEnd());
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_MAXIMIZEBOX);
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        await _vm.InitializeAsync();
    }
}
