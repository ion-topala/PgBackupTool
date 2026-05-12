using System.Windows;
using PgBackupTool.ViewModels;

namespace PgBackupTool.Views;

public partial class ProfileEditorWindow : Window
{
    private readonly ProfileEditorViewModel _vm;

    public ProfileEditorWindow(ProfileEditorViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        // Pre-fill PasswordBox (not bindable via standard MVVM)
        PasswordBox.Password = vm.Password;

        vm.CloseAction = () =>
        {
            // Harvest password on close-via-save
            vm.Password = PasswordBox.Password;
            Close();
        };
    }
}
