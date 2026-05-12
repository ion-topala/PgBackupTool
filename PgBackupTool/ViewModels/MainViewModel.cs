using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PgBackupTool.Models;
using PgBackupTool.Services;
using PgBackupTool.Views;

namespace PgBackupTool.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ProfileStore _profileStore = new();
    private readonly BackupService _backupService = new();
    private readonly RestoreService _restoreService = new();
    private readonly BackupCatalog _catalog = new();

    public BackupHistoryViewModel History { get; }

    [ObservableProperty] private ObservableCollection<ConnectionProfile> _profiles = [];
    [ObservableProperty] private ConnectionProfile? _selectedProfile;
    [ObservableProperty] private string _logText = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isCancelVisible;

    private CancellationTokenSource? _cts;

    public MainViewModel()
    {
        History = new BackupHistoryViewModel(_catalog);
    }

    partial void OnSelectedProfileChanged(ConnectionProfile? value) =>
        History.Refresh(value);

    public async Task InitializeAsync()
    {
        var loaded = await _profileStore.LoadAsync();
        Profiles = new ObservableCollection<ConnectionProfile>(loaded);
        SelectedProfile = Profiles.FirstOrDefault();
        CheckPgTools();
    }

    private void CheckPgTools()
    {
        try
        {
            using var proc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("pg_dump", "--version")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            proc?.WaitForExit(3000);
        }
        catch
        {
            MessageBox.Show(
                "pg_dump not found on PATH.\n\nPlease ensure PostgreSQL client tools are installed and available on PATH.",
                "PostgreSQL Tools Not Found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private async Task AddProfileAsync()
    {
        var vm = new ProfileEditorViewModel();
        var dlg = new ProfileEditorWindow(vm) { Owner = Application.Current.MainWindow };
        dlg.ShowDialog();
        if (!vm.Saved) return;

        var profile = vm.BuildProfile();
        Profiles.Add(profile);
        SelectedProfile = profile;
        await _profileStore.SaveAsync(Profiles);
    }

    [RelayCommand]
    private async Task RemoveProfileAsync()
    {
        if (SelectedProfile is null) return;

        var confirm = MessageBox.Show(
            $"Remove profile '{SelectedProfile.Name}'?",
            "Confirm Remove",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        Profiles.Remove(SelectedProfile);
        SelectedProfile = Profiles.FirstOrDefault();
        await _profileStore.SaveAsync(Profiles);
    }

    [RelayCommand]
    private async Task EditProfileAsync()
    {
        if (SelectedProfile is null) return;

        var vm = new ProfileEditorViewModel(SelectedProfile);
        var dlg = new ProfileEditorWindow(vm) { Owner = Application.Current.MainWindow };
        dlg.ShowDialog();
        if (!vm.Saved) return;

        var updated = vm.BuildProfile(SelectedProfile.Id);
        var idx = Profiles.IndexOf(SelectedProfile);
        Profiles[idx] = updated;
        SelectedProfile = updated;
        History.Refresh(updated);
        await _profileStore.SaveAsync(Profiles);
    }

    [RelayCommand]
    private async Task BackupNowAsync()
    {
        if (SelectedProfile is null || IsBusy) return;

        BeginOperation();
        AppendLog($"=== Backup started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");

        var progress = new Progress<string>(AppendLog);
        var result = await _backupService.CreateBackupAsync(SelectedProfile, progress, _cts!.Token);

        EndOperation();

        if (result.Success)
        {
            AppendLog($"=== Backup completed successfully ===");
            History.Refresh(SelectedProfile);
        }
        else
        {
            AppendLog($"=== Backup failed: {result.ErrorMessage} ===");
            MessageBox.Show(result.ErrorMessage, "Backup Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task RestoreAsync(object? parameter)
    {
        if (SelectedProfile is null || IsBusy) return;
        if (parameter is not BackupEntry entry) return;

        var confirm = MessageBox.Show(
            $"This will PERMANENTLY DROP the database '{SelectedProfile.Database}' on {SelectedProfile.Host}:{SelectedProfile.Port} " +
            $"and replace it with the backup from {entry.CreatedAt:yyyy-MM-dd HH:mm:ss}.\n\n" +
            $"This action cannot be undone.\n\nProceed?",
            "Confirm Restore",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirm != MessageBoxResult.Yes) return;

        BeginOperation();
        AppendLog($"=== Restore started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");

        var progress = new Progress<string>(AppendLog);
        var result = await _restoreService.RestoreAsync(SelectedProfile, entry.FilePath, progress, _cts!.Token);

        EndOperation();

        if (result.Success)
        {
            AppendLog("=== Restore completed successfully ===");
        }
        else
        {
            AppendLog($"=== Restore failed: {result.ErrorMessage} ===");
            MessageBox.Show(result.ErrorMessage, "Restore Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void DeleteBackup(object? parameter)
    {
        if (parameter is not BackupEntry entry) return;

        var confirm = MessageBox.Show(
            $"Delete backup file '{entry.FileName}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            _catalog.DeleteBackup(entry.FilePath);
            History.RemoveEntry(entry);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to delete: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
        AppendLog("[operation] Cancellation requested...");
    }

    private void BeginOperation()
    {
        _cts = new CancellationTokenSource();
        IsBusy = true;
        IsCancelVisible = true;
    }

    private void EndOperation()
    {
        IsBusy = false;
        IsCancelVisible = false;
        _cts?.Dispose();
        _cts = null;
    }

    private void AppendLog(string line)
    {
        LogText += line + Environment.NewLine;
    }
}
