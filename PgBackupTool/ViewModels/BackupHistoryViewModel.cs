using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PgBackupTool.Models;
using PgBackupTool.Services;

namespace PgBackupTool.ViewModels;

public partial class BackupHistoryViewModel : ObservableObject
{
    private readonly BackupCatalog _catalog;

    [ObservableProperty] private ObservableCollection<BackupEntry> _entries = [];

    public BackupHistoryViewModel(BackupCatalog catalog)
    {
        _catalog = catalog;
    }

    public void Refresh(ConnectionProfile? profile)
    {
        Entries = new ObservableCollection<BackupEntry>(_catalog.ListBackups(profile));
    }

    public void RemoveEntry(BackupEntry entry) => Entries.Remove(entry);
}
